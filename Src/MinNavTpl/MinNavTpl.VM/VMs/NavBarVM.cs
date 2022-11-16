namespace MinNavTpl.VM.VMs;
public partial class NavBarVM : BaseMinVM
{
  readonly SrvrNameStore _SrvrNameStore;
  readonly DtBsNameStore _DtBsNameStore;
  readonly GSReportStore _GSReportStore;
  readonly LetDbChgStore __letDbChStore;

  public NavBarVM(SrvrNameStore SrvrNameStore, DtBsNameStore DtBsNameStore, GSReportStore gsr, LetDbChgStore letDStore, Page00NavSvc page00NavSvc, Page01NavSvc page01NavSvc, Page02NavSvc page02NavSvc, Page03NavSvc page03NavSvc, Page04NavSvc page04NavSvc, Page05NavSvc Page05NavSvc, EmailDetailNavSvc emlDtlNavSvc, UserSettings usrStgns)
  {
    _SrvrNameStore = SrvrNameStore;
    _DtBsNameStore = DtBsNameStore;
    _GSReportStore = gsr;
    __letDbChStore = letDStore;

    _SrvrNameStore.Changed += SrvrNameStore_Chngd;
    _DtBsNameStore.Changed += DtBsNameStore_Chngd;
    //_GSReportStore.Changed += GSReportStore_Chngd;
    __letDbChStore.Changed += LetDbChgStore_Chngd;

    UsrStgns = usrStgns;
    NavigatePage00Command = new NavigateCommand(page00NavSvc);
    NavigatePage01Command = new NavigateCommand(page01NavSvc);
    NavigatePage02Command = new NavigateCommand(page02NavSvc);
    NavigatePage03Command = new NavigateCommand(page03NavSvc);
    NavigatePage04Command = new NavigateCommand(page04NavSvc);
    NavigatePage05Command = new NavigateCommand(Page05NavSvc);
    NavigateEmlDtlCommand = new NavigateCommand(emlDtlNavSvc);

    PrefSrvrName = usrStgns.SrvrName;
    PrefDtBsName = usrStgns.DtBsName;
    LetDbChg = usrStgns.LetDbChg;

    IsEnabledLetDbChg = true;

    IsDevDbg = VersionHelper.IsDbg;

    _awd = IsDevDbg && /*_secForcer.CanEdit &&*/ (
      UsrStgns.SrvrName is null ? false :
      UsrStgns.SrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UsrStgns.LetDbChg);
  }

  void SrvrNameStore_Chngd(string srvr) => PrefSrvrName = srvr;  //OnPropertyChanged(nameof(SrvrName)); }
  void DtBsNameStore_Chngd(string dtbs) => PrefDtBsName = dtbs;  //OnPropertyChanged(nameof(DtBsName)); }
  //void GSReportStore_Chngd(string dtbs) => PrefGSReport = dtbs;  //OnPropertyChanged(nameof(DtBsName)); }
  void LetDbChgStore_Chngd(bool value) => LetDbChg = value;

  bool _awd; public bool LetDbChg { get => _awd; set { if (SetProperty(ref _awd, value)) { UsrStgns.LetDbChg = value; __letDbChStore.Change(value); } } }
  [ObservableProperty] bool isEnabledLetDbChg;
  [ObservableProperty] string prefSrvrName = Consts.SqlServerList.First();
  [ObservableProperty] string prefDtBsName = ".\\SqlExpress";
  [ObservableProperty] string gSReportName = "...GSReport";
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible;

  public UserSettings UsrStgns { get; }

  public ICommand NavigatePage00Command { get; }
  public ICommand NavigatePage01Command { get; }
  public ICommand NavigatePage02Command { get; }
  public ICommand NavigatePage03Command { get; }
  public ICommand NavigatePage04Command { get; }
  public ICommand NavigatePage05Command { get; }
  public ICommand NavigateEmlDtlCommand { get; }

  IRelayCommand? _sq; public IRelayCommand SwtchSqlSvrCmd => _sq ??= new AsyncRelayCommand<object>(SwitchSqlServer); async Task SwitchSqlServer(object? sqlServerTLA)
  {
    ArgumentNullException.ThrowIfNull(sqlServerTLA, nameof(sqlServerTLA));

    PrefSrvrName = UsrStgns.SrvrName = Consts.SqlServerList.FirstOrDefault(r => r.Contains((string)sqlServerTLA, StringComparison.InvariantCultureIgnoreCase)) ?? Consts.SqlServerList.First();

    _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
    await Task.Delay(2600);
    System.Windows.Application.Current.Shutdown();
  }

  public override void Dispose()
  {
    _SrvrNameStore.Changed -= SrvrNameStore_Chngd;
    _DtBsNameStore.Changed -= DtBsNameStore_Chngd;
    _GSReportStore.Changed -= DtBsNameStore_Chngd;
    __letDbChStore.Changed -= LetDbChgStore_Chngd;

    base.Dispose();
  }
}