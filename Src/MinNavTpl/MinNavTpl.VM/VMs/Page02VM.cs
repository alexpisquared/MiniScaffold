using EF.DbHelper.Lib;
using MinNavTpl.Contract;
using MinNavTpl.VM.Stores;

namespace MinNavTpl.VM.VMs;
public class Page02VM : BaseDbVM, IPage02VMLtd
{
  readonly SqlPermissionsManager _spm;
  readonly DbConnection _dbConnection;
  public Page02VM(MainVM mainVM, INavSvc loginNavSvc, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, UserSettings usrStgns, SqlPermissionsManager spm, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110)
  {
    NavigateLoginCommand = new NavigateCommand(loginNavSvc);
    _dbConnection = new SqlConnection(string.Format(Config[GenConst.SqlVerSpm] ?? "!d", UserSetgs.PrefSrvrName, UserSetgs.PrefDtBsName));
    _spm = spm;
  }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;

      PrefAplctnId = UserSetgs.PrefAplctnId;
      
      await LoadEF();

      return await base.InitAsync();
    }
    finally { IsBusy = false; }
  }

  ICollectionView? _p; public ICollectionView? PermViewSource { get => _p; set => SetProperty(ref _p, value); }
  ICollectionView? _a; public ICollectionView? AplnViewSource { get => _a; set => SetProperty(ref _a, value); }
  ICollectionView? _u; public ICollectionView? UserViewSource { get => _u; set => SetProperty(ref _u, value); }

  bool _r = false; public bool AutoSyncToSqlRoles { get => _r; set => SetProperty(ref _r, value); }
  bool _m = false; public bool MemberFilter { get => _m; set { if (SetProperty(ref _m, value)) UserViewSource?.Refresh(); } }
  string _s = ""; public string SearchText { get => _s; set { if (SetProperty(ref _s, value)) UserViewSource?.Refresh(); } }
  int _n = -1; public int PrefAplctnId { get => _n; set { if (SetProperty(ref _n, value)) { UserSetgs.PrefAplctnId = value; PermViewSource?.Refresh(); } } }

  public ICommand NavigateLoginCommand { get; }

  RelayCommand? selectPermCommand; public ICommand SelectPermCommand => selectPermCommand ??= new RelayCommand(SelectPerm); void SelectPerm() { }

  public async Task LoadEF(bool isAsync = true)
  {
    try
    {
      var sw = Stopwatch.StartNew();

      if (isAsync)
      {
        await base.DbxInv.Applications.LoadAsync();
        await base.DbxInv.Permissions.LoadAsync();
        await DbxInv.Users.Where(r => r.UserId == null || !r.UserId.StartsWith("bbssecurities\\")).LoadAsync();
        await base.DbxInv.PermissionAssignments.LoadAsync();
      }
      else
      {
        base.DbxInv.Applications.Load();
        base.DbxInv.Permissions.Load();
        DbxInv.Users.Where(r => r.UserId == null || !r.UserId.StartsWith("bbssecurities\\")).Load();
        base.DbxInv.PermissionAssignments.Load();
      }

      UserViewSource = CollectionViewSource.GetDefaultView(base.DbxInv.Users.Local.ToObservableCollection());
      UserViewSource.SortDescriptions.Add(new SortDescription(nameof(User.UserId), ListSortDirection.Ascending));
      UserViewSource.Filter = obj => obj is not User user || user is null || (
        (!MemberFilter || user.Granted == MemberFilter) &&
        (string.IsNullOrEmpty(SearchText) || user.UserId?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true));

      PermViewSource = CollectionViewSource.GetDefaultView(base.DbxInv.Permissions.Local.ToObservableCollection());
      PermViewSource.SortDescriptions.Add(new SortDescription(nameof(Permission.Name), ListSortDirection.Ascending)); //tu: instead of  .OrderBy(r => r.UserId); lest forfeit CanUserAddRows.
      PermViewSource.Filter = obj => PrefAplctnId <= 0 || (obj is Permission perm && perm.AppId == PrefAplctnId);

      AplnViewSource = CollectionViewSource.GetDefaultView(base.DbxInv.Applications.Local.ToObservableCollection());
      AplnViewSource.SortDescriptions.Add(new SortDescription(nameof(DB.Inventory.Models.Application.AppName), ListSortDirection.Ascending));

      Logger.Log(LogLevel.Trace, $"│   LoadEF():{sw.Elapsed.TotalSeconds,5:N1}s for DB  {UserSetgs.PrefSrvrName,-12}:\t {new string('■', (int)sw.Elapsed.TotalSeconds)} ");
    }
    catch (Exception ex) { ex.Pop(Logger); }
    //finally { _vm.Bpr?.Tick(); }
  }

  public void DoDgSelChngdPerm(Permission lastSelectPerm)
  {
    base.DbxInv.Permissions.Local.ToList().ForEach(r => { r.Granted = null; r.Selectd = false; });

    base.DbxInv.Users.Local.ToList().ForEach(r => r.Granted = false);

    foreach (var pa in lastSelectPerm.PermissionAssignments) { var u = base.DbxInv.Users.Local.FirstOrDefault(r => r.UserIntId == pa.UserId); if (u != null) u.Granted = true; }

    base.DbxInv.Permissions.Local.ToList().ForEach(r => r.Granted = null);
    base.DbxInv.Users.Local.ToList().ForEach(r => r.Selectd = false);

    UserViewSource?.Refresh(); // members-only check support.
  }
  public void DoDgSelChngdUser(User lastSelectUser)
  {
    base.DbxInv.Users.Local.ToList().ForEach(r => { r.Granted = null; r.Selectd = false; });      //CollectionViewSource.GetDefaultView(dgUser.ItemsSource).Refresh(); //tu: refresh bound datagrid
    base.DbxInv.Permissions.Local.ToList().ForEach(r => r.Granted = false);

    foreach (var pa in lastSelectUser.PermissionAssignments) { var p = base.DbxInv.Permissions.Local.FirstOrDefault(r => r.PermissionId == pa.PermissionId); if (p != null) p.Granted = true; }

    base.DbxInv.Users.Local.ToList().ForEach(r => r.Granted = null);
    base.DbxInv.Permissions.Local.ToList().ForEach(r => r.Selectd = false);
  }

  ICommand? _rf; public ICommand RefreshFromDbCommand => _rf ??= new AsyncRelayCommand(RefreshFromDb); async Task RefreshFromDb()
  {
    await Task.Yield(); ;
  }

  public async Task<int> SyncToSqlAdd(PermissionAssignment pa)
  {
    if (AutoSyncToSqlRoles)
      return await _spm.AddUserToRole(pa.User?.UserId ?? throw new ArgumentNullException(nameof(pa)), $"{pa.Permission.App.AppName}.{pa.Permission.Name}", _dbConnection);
    else
      return -9871;
  }
  public async Task<int> SyncToSqlRmv(PermissionAssignment pa)
  {
    if (AutoSyncToSqlRoles)
      return await _spm.RmvUserFrRole(pa.User?.UserId ?? throw new ArgumentNullException(nameof(pa)), $"{pa.Permission.App.AppName}.{pa.Permission.Name}", _dbConnection);
    else
      return -9871;
  }

  void IPage02VMLtd.CheckDb() => throw new NotImplementedException();
}