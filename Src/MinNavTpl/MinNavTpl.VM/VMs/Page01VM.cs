using MinimalNavTemplate.VM.Stores;

namespace MinimalNavTemplate.VM.VMs;
public class Page01VM : BaseDbVM
{
  readonly SqlPermissionsManager _spm;
  readonly BmsPermissionsManager _bpm;
  const string _startUpDB = "Inventory";
  readonly string _constr;
  bool _isDbsLoaded;

  public Page01VM(MainVM mainVM, SqlPermissionsManager spm, BmsPermissionsManager bpm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, SrvrStore srvrStore, DtBsStore dtbsStore, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110)
  {
    _spm = spm;
    _bpm = bpm;

    SrvrStore = srvrStore; SrvrStore.CurrentSrvrChanged += SrvrStore_SrvrChngd;
    DtBsStore = dtbsStore; DtBsStore.CurrentDtbsChanged += DtbsStore_DtbsChngd;

    _sl = DevOps.IsDevMachineH ? new ObservableCollection<ADSrvr> {
        new ADSrvr(@".\sqlexpress", "DEV", "DEV"),
        new ADSrvr(@".\SqlExpress", "UAT", "UAT"),
        new ADSrvr(@".\SQLEXPRESS", "PRD", "PRD")
      } : DevOps.IsDevMachineO ? new ObservableCollection<ADSrvr> {
        new ADSrvr("mtDEVsqldb", "DEV", "DEV"),
        new ADSrvr("mtDEVsqldb,1625", "DEV", "DEV"),
        new ADSrvr("mtUATsqldb", "UAT", "UAT"),
        //new ADSrvr("mtPRDsqldb", "PRD", "PRD")
      } : new ObservableCollection<ADSrvr> {
        new ADSrvr("MTDEVSQLDB,1625", "DEV", "DEV"),
        new ADSrvr("MTUATSQLDB,1625", "UAT", "UAT"),
        //new ADSrvr("MTPRDSQLDB", "PRD", "PRD")
      };

    _dl = new ObservableCollection<ADDtBs> { new ADDtBs(_startUpDB, "", "", DateTime.Now) }; // use Inventory first for ConStr just to load all Dbs.
    _rl = new ObservableCollection<ADRole> { new ADRole("← Select Database first", "") };

    _constr = string.Format(Config[GenConst.SqlVerSpm] ?? "!d", UserSetgs.PrefSrvrName, UserSetgs.PrefDtBsName);

    _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Logger); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
  }

  public SrvrStore SrvrStore { get; }
  public DtBsStore DtBsStore { get; }
  async void SrvrStore_SrvrChngd(ADSrvr srvr) { SelectSrvr = srvr; await RefreshReloadAsync(); }
  async void DtbsStore_DtbsChngd(ADDtBs srvr) { SelectDtBs = srvr; await RefreshReloadAsync(); }

  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var swAD = Stopwatch.StartNew();
      var adus = await LdapHelper.GetAllAsync();
      Logger.Log(LogLevel.Trace, $"│{swAD.Elapsed.TotalSeconds,6:N1}s  {adus.Count,9:N0} AD users loaded in {swAD.Elapsed.TotalSeconds,6:N1}s:\t {new string('■', (int)swAD.Elapsed.TotalSeconds)} ");

      var swDb = Stopwatch.StartNew();
      var lgns = await _spm.GetSvrLgns(new SqlConnection(_constr));

      adus.Where(user => lgns.Contains(user.DomainUsername, new IgnoreCaseComparer()) == true).ToList().ForEach(user => { user.TopDbRole = IpmUserRole.LognOnly; user.L = true; });

      ADUserCVS = CollectionViewSource.GetDefaultView(adus);
      ADUserCVS.SortDescriptions.Add(new SortDescription(nameof(ADUser.FullName), ListSortDirection.Ascending));
      ADUserCVS.Filter = obj => obj is not ADUser user || user is null || (
        (!_f || user.IsMemberOfGivenRole == _f) &&
        (!_l || user.L == _l) &&
        (string.IsNullOrEmpty(SearchText) || user.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || user.SamAccountName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

      _ = await Page01VMHelpers.LoadSrvrDtBssTask(UserSetgs.PrefSrvrName, DtBsList, SetLoadedFlag, _constr, Logger);
      _ = await Page01VMHelpers.LoadDtBsRolesTask(RoleList, _spm, _constr, Logger);

      if (((Collection<ADSrvr>)_sl).Any()) SelectSrvr = ((Collection<ADSrvr>)_sl).FirstOrDefault(r => r.Name == UserSetgs.PrefSrvrName) ?? ((Collection<ADSrvr>)_sl).First();
      if (((Collection<ADDtBs>)_dl).Any()) SelectDtBs = ((Collection<ADDtBs>)_dl).FirstOrDefault(r => r.Name == UserSetgs.PrefDtBsName) ?? ((Collection<ADDtBs>)_dl).First();
      if (((Collection<ADRole>)_rl).Any()) SelectRole = ((Collection<ADRole>)_rl).FirstOrDefault(r => r.Name == UserSetgs.PrefDtBsRole) ?? ((Collection<ADRole>)_rl).First();

      Logger.Log(LogLevel.Trace, $"│{swDb.Elapsed.TotalSeconds,6:N1}s  {lgns.Count,9:N0} DBLogins loaded in {swDb.Elapsed.TotalSeconds,6:N1}s:\t {new string('■', (int)swDb.Elapsed.TotalSeconds)} \t at {UserSetgs.PrefSrvrName}");
      return true;
    }
    catch (Exception ex) { ex.Pop(Logger); return false; }
    finally
    {
      IsBusy = false;
      _ = await base.InitAsync();
    }
  }
  public override Task<bool> WrapAsync()
  {
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    return base.WrapAsync();
  }
  bool _f; public bool MemberFilter { get => _f; set { if (SetProperty(ref _f, value)) { ADUserCVS?.Refresh(); } } }
  bool _b; public bool BmsSynchrony { get => _b; set { if (SetProperty(ref _b, value)) { ADUserCVS?.Refresh(); } } }
  bool _l; public bool SqlLoginOnly { get => _l; set { if (SetProperty(ref _l, value)) { ADUserCVS?.Refresh(); } } }
  string _xh = ""; public string SearchText { get => _xh; set { if (SetProperty(ref _xh, value)) ADUserCVS?.Refresh(); } }
  IEnumerable _sl; public IEnumerable SrvrList { get => _sl; set => SetProperty(ref _sl, value); }
  IEnumerable _dl; public IEnumerable DtBsList { get => _dl; set => SetProperty(ref _dl, value); }
  IEnumerable _rl; public IEnumerable RoleList { get => _rl; set => SetProperty(ref _rl, value); }
  ICollectionView? _cv1; public ICollectionView? ADUserCVS { get => _cv1; set => SetProperty(ref _cv1, value); }

  ADSrvr? _cs; public ADSrvr? SelectSrvr
  {
    get => _cs; set
    {
      if (SetProperty(ref _cs, value) && value is not null && _inited)
      {
        try
        {
          Bpr.Click();          //ArgumentNullException.ThrowIfNull(value, nameof(value));
          UserSetgs.PrefSrvrName = value.Name;
          SrvrStore.ChgSrvr(value);
#if true
          _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
          _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
          {
            await Task.Delay(2600);
            System.Windows.Application.Current.Shutdown();
          });
#else
          Page01VMHelpers.LoadServerDBsFAF(value.Name, DtBsList, SetLoadedFlag, string.Format(Config[GenConst.SqlVer] ?? "d", value.Name, _startUpDB), Logger);
#endif
        }
        catch (Exception ex) { ex.Pop(Logger); }
      }
    }
  }
  ADDtBs? _cd; public ADDtBs? SelectDtBs
  {
    get => _cd; set
    {
      if (SetProperty(ref _cd, value) && value is not null && _isDbsLoaded && _inited)
      {
        Bpr.Click();
        UserSetgs.PrefDtBsName = value.Name;
        DtBsStore.ChgDtBs(value);
        Page01VMHelpers.LoadDtBsRolesFAF(value.Name, RoleList, _spm, string.Format(Config[GenConst.SqlVerSpm] ?? "'", SelectSrvr?.Name, SelectDtBs?.Name), Logger);
      }
    }
  }
  ADRole? _cr; public ADRole? SelectRole
  {
    get => _cr; set
    {
      if (SetProperty(ref _cr, value) && value is not null && _isDbsLoaded)
      {
        Bpr.Click();
        UserSetgs.PrefDtBsRole = value.Name;
        Page01VMHelpers.MarkRoleUsersFAF(value.Name, ADUserCVS, _spm, string.Format(Config[GenConst.SqlVerSpm] ?? "e", SelectSrvr?.Name, SelectDtBs?.Name));
        IsMemberOf = $"{value?.Name ?? @"<select desired Server\Database\Role>"}";
      }
    }
  }
  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }
  string _im = "^^ Select desired\nServer\\Database\\Role"; public string IsMemberOf { get => _im; set => SetProperty(ref _im, value); }
  string _dq = "_Database"; public string DbCountMsg { get => _dq; set => SetProperty(ref _dq, value); }
  string _rq = "_Role    "; public string RoleCntMsg { get => _rq; set => SetProperty(ref _rq, value); }

  void SetLoadedFlag(int dbCnt)
  {
    _isDbsLoaded = true;
    DbCountMsg = $"_Database  ({dbCnt})";
  }

  ICommand? _cn; public ICommand SetToNoneCommand => _cn ??= new AsyncRelayCommand<object>(SetToNone); async Task<int> SetToNone(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.LognOnly);
  ICommand? _sm; public ICommand SetMemberCommand => _sm ??= new AsyncRelayCommand<object>(SetMember); async Task<int> SetMember(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.ReadOnly);
  async Task<int> SetToRole(object? isAddObj, string dbRole)
  {
    if (CurentUser is null) return -2;
    if (SelectRole is null) return -9;
    if (isAddObj is not bool isAdd) throw new ArgumentException(nameof(SetToRole), nameof(isAddObj));

    var report = $"{CurentUser.DomainUsername,-32}  {(isAdd ? "+joins+ " : "-leaves-")}  {SelectRole}  DB Role.  ▓▓  ▒▒  ░░  ■■  ■■  ■■  ──  ──  ──";
    try
    {
      CurentUser.TopDbRole = SelectRole.Name; //tu: !!! use CurrentItem instead of SelectedItem !!! otherwise: changing anything on CurentUser breaks binding to CurentUser !!!>???

      var dbConnection = new SqlConnection(_constr);

      if (SelectRole.Name == IpmUserRole.LognOnly)
      {

        if (isAdd)
        {
          _ = await _spm.CreateSqlLoginAndDbUser(CurentUser.DomainUsername, dbConnection);
        }
        else
        {
          if (CurentUser.A) _ = await SetToRole(isAddObj, IpmUserRole.IpmAdmin);
          if (CurentUser.W) _ = await SetToRole(isAddObj, IpmUserRole.WorkUser);
          if (CurentUser.R) _ = await SetToRole(isAddObj, IpmUserRole.ReadOnly);

          _ = await _spm.DropDbUserAndSqlLogin(CurentUser.DomainUsername, dbConnection);
        }
      }
      else
      {
        _ = isAdd ?
          await _spm.AddUserToRole(CurentUser.DomainUsername, SelectRole.Name, dbConnection) :
          await _spm.RmvUserFrRole(CurentUser.DomainUsername, SelectRole.Name, dbConnection);

        if (BmsSynchrony)
        {
          var ra = await _bpm.AlterRole(isAdd, CurentUser.DomainUsername, SelectRole.Name);
          report += $"  BMS: {ra} rows affected.";
        }
      }

      switch (SelectRole.Name)
      {
        case IpmUserRole.IpmAdmin: CurentUser.A = isAdd; break;
        case IpmUserRole.WorkUser: CurentUser.W = isAdd; break;
        case IpmUserRole.ReadOnly: CurentUser.R = isAdd; break;
        case IpmUserRole.LognOnly: CurentUser.L = isAdd; break;
      }

      Logger.LogInformation(report);
      return 0;
    }
    catch (Exception ex) { ex.Pop(Logger); return -7; }
    finally { IsBusy = false; Bpr.Tick(); }
  }

  public override void Dispose()
  {
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    DtBsStore.CurrentDtbsChanged -= DtbsStore_DtbsChngd;

    base.Dispose();
  }
}