namespace MinNavTpl.VM.VMs;
public partial class NavBarVM : BaseMinVM
{
  readonly SrvrStore _srvrStore;
  readonly DtBsStore _dtbsStore;
  readonly AllowWriteDBStore _allowWriteDBStore;

  public NavBarVM(SrvrStore srvrStore, DtBsStore dtbsStore, AllowWriteDBStore allowWriteDBStore, Page01NavSvc page01NavSvc, Page02NavSvc page02NavSvc, Page03NavSvc page03NavSvc, UserSettings usrStgns)
  {
    _srvrStore = srvrStore;
    _dtbsStore = dtbsStore;
    _allowWriteDBStore = allowWriteDBStore;

    _srvrStore.CurrentSrvrChanged += OnCurrentSrvrChanged;
    _dtbsStore.CurrentDtbsChanged += OnCurrentDtbsChanged;
    _allowWriteDBStore.AllowWriteDBChanged += _allowWriteDBStore_AllowWriteDBChanged;

    UsrStgns = usrStgns;
    NavigatePage01Command = new NavigateCommand(page01NavSvc);
    NavigatePage02Command = new NavigateCommand(page02NavSvc);
    NavigatePage03Command = new NavigateCommand(page03NavSvc);

    PrefSrvrName = usrStgns.PrefSrvrName;
    PrefDtBsName = usrStgns.PrefDtBsName;
    AllowWriteDB = usrStgns.AllowWriteDB;

    IsEnabledAllowWriteDB = true;

    IsDevDbg = VersionHelper.IsDbg;

    _awd = IsDevDbg && /*_secForcer.CanEdit &&*/ (
      UsrStgns.PrefSrvrName is null ? false :
      UsrStgns.PrefSrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UsrStgns.AllowWriteDB);
  }

  void _allowWriteDBStore_AllowWriteDBChanged(bool val) { AllowWriteDB = val; ; }
  void OnCurrentSrvrChanged(ADSrvr srvr) => PrefSrvrName = srvr.Name;  //OnPropertyChanged(nameof(PrefSrvrName)); }
  void OnCurrentDtbsChanged(ADDtBs dtbs) => PrefDtBsName = dtbs.Name;  //OnPropertyChanged(nameof(PrefDtBsName)); }

  bool _awd; public bool AllowWriteDB { get => _awd; set { if (SetProperty(ref _awd, value)) { UsrStgns.AllowWriteDB = value; _allowWriteDBStore.ChangAllowWriteDB(value); } } }
  [ObservableProperty] bool isEnabledAllowWriteDB;
  [ObservableProperty] string prefSrvrName = Consts.SqlServerList.First();
  [ObservableProperty] string prefDtBsName = ".\\SqlExpress";
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible;

  public UserSettings UsrStgns { get; }

  public ICommand NavigatePage01Command { get; }
  public ICommand NavigatePage02Command { get; }
  public ICommand NavigatePage03Command { get; }

  IRelayCommand? _sq; public IRelayCommand SwtchSqlSvrCmd => _sq ??= new AsyncRelayCommand<object>(SwitchSqlServer); async Task SwitchSqlServer(object? sqlServerTLA)
  {
    ArgumentNullException.ThrowIfNull(sqlServerTLA, nameof(sqlServerTLA));

    PrefSrvrName = UsrStgns.PrefSrvrName = Consts.SqlServerList.FirstOrDefault(r => r.Contains((string)sqlServerTLA, StringComparison.InvariantCultureIgnoreCase)) ?? Consts.SqlServerList.First();

    _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
    await Task.Delay(2600);
    System.Windows.Application.Current.Shutdown();
  }

  public override void Dispose()
  {
    _srvrStore.CurrentSrvrChanged -= OnCurrentSrvrChanged;
    _dtbsStore.CurrentDtbsChanged -= OnCurrentDtbsChanged;
    _allowWriteDBStore.AllowWriteDBChanged -= _allowWriteDBStore_AllowWriteDBChanged;

    base.Dispose();
  }
}