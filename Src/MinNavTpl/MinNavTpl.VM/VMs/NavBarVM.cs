namespace MinNavTpl.VM.VMs;
public partial class NavBarVM : BaseMinVM
{
  readonly SrvrNameStore _srvrStore;
  readonly DtBsNameStore _dtbsStore;
  readonly LetDbChgStore _letDStore;

  public NavBarVM(SrvrNameStore srvrStore, DtBsNameStore dtbsStore, LetDbChgStore letDStore, Page00NavSvc page00NavSvc, Page01NavSvc page01NavSvc, Page02NavSvc page02NavSvc, Page03NavSvc page03NavSvc, Page04NavSvc page04NavSvc, UserSettings usrStgns)
  {
    _srvrStore = srvrStore;
    _dtbsStore = dtbsStore;
    _letDStore = letDStore;

    _srvrStore.Changed += OnCurrentSrvrChanged;
    _dtbsStore.Changed += OnCurrentDtbsChanged;
    _letDStore.Changed += OnCurrentLetDChanged;

    UsrStgns = usrStgns;
    NavigatePage00Command = new NavigateCommand(page00NavSvc);
    NavigatePage01Command = new NavigateCommand(page01NavSvc);
    NavigatePage02Command = new NavigateCommand(page02NavSvc);
    NavigatePage03Command = new NavigateCommand(page03NavSvc);
    NavigatePage04Command = new NavigateCommand(page04NavSvc);

    PrefSrvrName = usrStgns.SrvrName;
    PrefDtBsName = usrStgns.DtBsName;
    LetDbChgProp = usrStgns.LetDbChg;

    IsEnabledLetDbChgProp = true;

    IsDevDbg = VersionHelper.IsDbg;

    _awd = IsDevDbg && /*_secForcer.CanEdit &&*/ (
      UsrStgns.SrvrName is null ? false :
      UsrStgns.SrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UsrStgns.LetDbChg);
  }

  void OnCurrentSrvrChanged(string srvr) => PrefSrvrName = srvr;  //OnPropertyChanged(nameof(SrvrName)); }
  void OnCurrentDtbsChanged(string dtbs) => PrefDtBsName = dtbs;  //OnPropertyChanged(nameof(DtBsNameProp)); }
  void OnCurrentLetDChanged(bool value) => LetDbChgProp = value;

  bool _awd; public bool LetDbChgProp { get => _awd; set { if (SetProperty(ref _awd, value)) { UsrStgns.LetDbChg = value; _letDStore.Change(value); } } }
  [ObservableProperty] bool isEnabledLetDbChgProp;
  [ObservableProperty] string prefSrvrName = Consts.SqlServerList.First();
  [ObservableProperty] string prefDtBsName = ".\\SqlExpress";
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible;

  public UserSettings UsrStgns { get; }

  public ICommand NavigatePage00Command { get; }
  public ICommand NavigatePage01Command { get; }
  public ICommand NavigatePage02Command { get; }
  public ICommand NavigatePage03Command { get; }
  public ICommand NavigatePage04Command { get; }

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
    _srvrStore.Changed -= OnCurrentSrvrChanged;
    _dtbsStore.Changed -= OnCurrentDtbsChanged;
    _letDStore.Changed -= OnCurrentLetDChanged;

    base.Dispose();
  }
}