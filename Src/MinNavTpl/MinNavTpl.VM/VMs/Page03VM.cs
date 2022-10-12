using MinimalNavTemplate.VM.Stores;

namespace MinimalNavTemplate.VM.VMs;
public class Page03VM : BaseDbVM
{
  readonly SqlPermissionsManager _spm;
  readonly BmsPermissionsManager _bpm;
  readonly string _constr;

  public Page03VM(MainVM mainVM, SqlPermissionsManager spm, BmsPermissionsManager bpm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110)
  {
    _spm = spm;
    _bpm = bpm;
    BmsIntegration = false;  
    _constr = string.Format(Config[GenConst.SqlVerSpm] ?? "!d", UserSetgs.PrefSrvrName, UserSetgs.PrefDtBsName);
  }
  public override async Task<bool> InitAsync() { await InitAsyncLocal(); await base.InitAsync();    return true;  }
  async Task InitAsyncLocal()
  {
    if (_loading) return;

    IsBusy = _loading = true;
    var sw = Stopwatch.StartNew();

    try
    {
      var dbConnection = new SqlConnection(_constr);

      var adus = (await LdapHelper.GetAllAsync()).ToList();
      var lgns = await _spm.GetSvrLgns(dbConnection);
      var reas = await _spm.GetUsersForRole(dbConnection, IpmUserRole.ReadOnly);
      var wrts = await _spm.GetUsersForRole(dbConnection, IpmUserRole.WorkUser);
      var adms = await _spm.GetUsersForRole(dbConnection, IpmUserRole.IpmAdmin);

      adus.Where(user => lgns.Contains(user.DomainUsername, new IgnoreCaseComparer()) == true).ToList().ForEach(user => { user.TopDbRole = IpmUserRole.LognOnly; user.L = true; });
      adus.Where(user => reas.Contains(user.DomainUsername, new IgnoreCaseComparer()) == true).ToList().ForEach(user => { user.TopDbRole = IpmUserRole.ReadOnly; user.R = true; });
      adus.Where(user => wrts.Contains(user.DomainUsername, new IgnoreCaseComparer()) == true).ToList().ForEach(user => { user.TopDbRole = IpmUserRole.WorkUser; user.W = true; });
      adus.Where(user => adms.Contains(user.DomainUsername, new IgnoreCaseComparer()) == true).ToList().ForEach(user => { user.TopDbRole = IpmUserRole.IpmAdmin; user.A = true; });

      ADUserCVS = CollectionViewSource.GetDefaultView(adus);
      ADUserCVS.SortDescriptions.Add(new SortDescription(nameof(ADUser.FullName), ListSortDirection.Ascending));
      ADUserCVS.Filter = obj => obj is not ADUser user || user is null || (
        (!_a || user.A == _a) &&
        (!_w || user.W == _w) &&
        (!_r || user.R == _r) &&
        (!_l || user.L == _l) &&
        (string.IsNullOrEmpty(SearchText) || user.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

      FilterLogn = true;

      Report = $"{VersionHelper.TimeAgo(sw.Elapsed, small: true)}";
      Logger.LogInformation($"│ {nameof(Page03VM)}.{nameof(InitAsync)}(): {Report.Replace("\n", " ")} \t {new string('■', (int)sw.Elapsed.TotalSeconds)} ");
    }
    catch (Exception ex) { IsBusy = false; ex.Pop(Logger); }
    finally { IsBusy = _loading = false; _inited = true; Bpr.Tick(); }
  }
  public override async Task RefreshReloadAsync([CallerMemberName] string? cmn = "")
  {
    WriteLine($"TrWL:> ###### Caller> {cmn}->Page03VM.RefreshReloadAsync() ");
    await InitAsyncLocal(); // refresh on YOI change.
    await base.RefreshReloadAsync();
  }

  bool _b; public bool BmsIntegration { get => _b; set => SetProperty(ref _b, value); }
  bool _a; public bool FilterAdmn { get => _a; set { if (SetProperty(ref _a, value)) { ADUserCVS?.Refresh(); } } }
  bool _w; public bool FilterWork { get => _w; set { if (SetProperty(ref _w, value)) { ADUserCVS?.Refresh(); } } }
  bool _r; public bool FilterRead { get => _r; set { if (SetProperty(ref _r, value)) { ADUserCVS?.Refresh(); } } }
  bool _l; public bool FilterLogn { get => _l; set { if (SetProperty(ref _l, value)) { ADUserCVS?.Refresh(); } } }
  string _xh = ""; public string SearchText { get => _xh; set { if (SetProperty(ref _xh, value)) ADUserCVS?.Refresh(); } }
  ADUser? _su; public ADUser? SelectUsr { get => _su; set { _ = SetProperty(ref _su, value); if (_su != null) WriteLine($"TrWL:> Select User:  {_su.FullName,-26} {_su.A,-6}{_su.W,-6}{_su.R,-6}{_su.L,-6}  {_su.Permisssions}"); } }
  ICollectionView? _cv1; public ICollectionView? ADUserCVS { get => _cv1; set => SetProperty(ref _cv1, value); }

  IRelayCommand? _ca; public IRelayCommand SetToAdmnCommand => _ca ??= new AsyncRelayCommand<object>(SetToAdmn); async Task<int> SetToAdmn(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.IpmAdmin);
  IRelayCommand? _cp; public IRelayCommand SetToUserCommand => _cp ??= new AsyncRelayCommand<object>(SetToUser); async Task<int> SetToUser(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.WorkUser);
  IRelayCommand? _cr; public IRelayCommand SetToReadCommand => _cr ??= new AsyncRelayCommand<object>(SetToRead); async Task<int> SetToRead(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.ReadOnly);
  IRelayCommand? _cn; public IRelayCommand SetToNoneCommand => _cn ??= new AsyncRelayCommand<object>(SetToNone); async Task<int> SetToNone(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.LognOnly);
  async Task<int> SetToRole(object? isAddObj, string dbRole)
  {
    if (SelectUsr is null) return -2; //nogo: { await Task.Delay(333); WriteLine($"▒▒ {dbRole,-20}  {isAddObj,-8} ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒"); }

    if (isAddObj is not bool isAdd) throw new ArgumentException(nameof(SetToUser), nameof(isAddObj));

    var report = $"{SelectUsr.DomainUsername,-32}  {(isAdd ? "+joins+ " : "-leaves-")}  {dbRole}  DB Role.  ▓▓  ▒▒  ░░  ■■  ■■  ■■  ──  ──  ──";

    try
    {
      var dbConnection = new SqlConnection(_constr);

      SelectUsr.TopDbRole = dbRole; //tu: !!! use CurrentItem instead of SelectedItem !!! otherwise: changing anything on SelectUsr breaks binding to SelectUsr !!!>???

      if (isAdd)
      {
        await _spm.MakeSureExistsDbRole(dbRole, dbConnection);
      }

      if (dbRole == IpmUserRole.LognOnly)
      {
        if (isAdd)
          _ = await _spm.CreateSqlLoginAndDbUser(SelectUsr.DomainUsername, dbConnection);
        else
          _ = await _spm.DropDbUserAndSqlLogin(SelectUsr.DomainUsername, dbConnection);
      }
      else
      {
        _ = await _spm.AlterRole(isAdd, SelectUsr.DomainUsername, dbRole, dbConnection);
        if (BmsIntegration)
        {
          var ra = await _bpm.AlterRole(isAdd, SelectUsr.DomainUsername, dbRole);
          report += $"  BMS: {ra} rows affected.";
        }
      }

      switch (dbRole)
      {
        case IpmUserRole.IpmAdmin: SelectUsr.A = isAdd; break;
        case IpmUserRole.WorkUser: SelectUsr.W = isAdd; break;
        case IpmUserRole.ReadOnly: SelectUsr.R = isAdd; break;
        case IpmUserRole.LognOnly: SelectUsr.L = isAdd; break;
      }

      Logger.LogInformation(report);
      return 0;
    }
    catch (Exception ex) { ex.Pop(Logger); return -7; }
    finally { IsBusy = false; Bpr.Tick(); }
  }
}