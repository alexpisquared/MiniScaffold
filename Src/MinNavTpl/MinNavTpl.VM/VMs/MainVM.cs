namespace MinNavTpl.VM.VMs;
public partial class MainVM : BaseMinVM
{
  readonly NavigationStore _navigationStore;
  public MainVM(NavBarVM navBarVM, NavigationStore navigationStore, ILogger lgr, IBpr bpr, IConfigurationRoot cfg, SrvrNameStore svr, DtBsNameStore dbs, EmailOfIStore eml, LetDbChgStore alw, UserSettings usr) : base()
  {
    NavBarVM = navBarVM;
    _navigationStore = navigationStore;
    Logger = lgr;
    Bpr = bpr;
    UsrStgns = usr;

    IsDevDbg = VersionHelper.IsDbg;

    _navigationStore.CurrentVMChanged += OnCurrentVMChanged;

    SrvrStore = svr; SrvrStore.Changed += SrvrStore_Chngd;
    DtBsStore = dbs; DtBsStore.Changed += DtbsStore_Chngd;
    EmaiStore = eml; EmaiStore.Changed += EmaiStore_Chngd;
    _letStore = alw; _letStore.Changed += LetCStore_Chngd;

    cfg[CfgName.ServerLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => SqlServrs.Add(r));
    cfg[CfgName.DtBsNmLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => DtBsNames.Add(r));

    Bpr.SuppressTicks = Bpr.SuppressAlarm = !(IsAudible = UsrStgns.IsAudible);
    IsAnimeOn = UsrStgns.IsAnimeOn;

    Bpr.Start();

    _ctored = true;
  }
  public override async Task<bool> InitAsync()
  {
    SrvrNameProp = UsrStgns.SrvrName;
    DtBsNameProp = UsrStgns.DtBsName;
    EmailOfIProp = UsrStgns.EmailOfI;
    LetDbChgProp = UsrStgns.LetDbChg;

    AppVerNumber = VersionHelper.CurVerStr("0.M.d");
    AppVerToolTip = VersionHelper.CurVerStr("0.M.d.H.m");

    var rv = await base.InitAsync();

    try { await KeepCheckingForUpdatesAndNeverReturn(); } catch (Exception ex) { ex.Pop(Logger); }

    return rv;
  }
  public override void Dispose()
  {
    NavBarVM.Dispose();

    _navigationStore.CurrentVMChanged -= OnCurrentVMChanged;

    SrvrStore.Changed -= SrvrStore_Chngd;
    DtBsStore.Changed -= DtbsStore_Chngd;
    EmaiStore.Changed -= EmaiStore_Chngd;
    _letStore.Changed -= LetCStore_Chngd;

    base.Dispose();
  }
  async Task KeepCheckingForUpdatesAndNeverReturn()
  {
    await Task.Delay(15000);
    OnCheckForNewVersion();

    var nextHour = DateTime.Now.AddHours(1);
    var nextHH00 = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 5);
    await Task.Delay(nextHH00 - DateTime.Now);
    OnCheckForNewVersion(true);

    while (await new PeriodicTimer(TimeSpan.FromMinutes(10)).WaitForNextTickAsync()) { OnCheckForNewVersion(); }
  }
  void OnCheckForNewVersion(bool logNetVer = false)
  {
    try
    {
      if (!File.Exists(DeploymntSrcExe))
      {
        Logger.LogWarning($"│   Version check    File does not exist:   {DeploymntSrcExe}   ***********************************************");
        AppVerToolTip = "Version check failed: depl. file is not found.";
        return;
      }

      (IsObsolete, var setupExeTime) = VersionHelper.CheckForNewVersion(DeploymntSrcExe);
      Logger.Log(IsObsolete ? LogLevel.Warning : LogLevel.Information, $"│   Version check this/depl {VersionHelper.TimedVer:MMdd·HHmm}{(IsObsolete ? "!=" : "==")}{setupExeTime:MMdd·HHmm}   {(IsObsolete ? "Obsolete    ▀▄▀▄▀▄▀▄▀▄▀▄▀" : "The latest  ─╬─  ─╬─  ─╬─")}   .n:{(logNetVer ? VersionHelper.DotNetCoreVersionCmd() : "[skipped]")}   ");

      UpgradeUrgency = .6 + Math.Abs((VersionHelper.TimedVer - setupExeTime).TotalDays);
      AppVerToolTip = IsObsolete ? $" New version is available:   0.{setupExeTime:M.d.HHmm} \n\t         from  {setupExeTime:yyyy-MM-dd HH:mm}.\n Click to update. " : $" This is the latest version  {VersionHelper.CurVerStrYYMMDD} \n\t               from  {VersionHelper.TimedVer:yyyy-MM-dd HH:mm}. ";
    }
    catch (Exception ex) { Logger.LogError(ex, "│   ▄─▀─▄─▀─▄ -- Ignore"); }
  }


  protected readonly LetDbChgStore _letStore;
  public SrvrNameStore SrvrStore { get; }
  public DtBsNameStore DtBsStore { get; }
  public EmailOfIStore EmaiStore { get; }
  void SrvrStore_Chngd(string val) { SrvrNameProp = val;   /* await RefreshReloadAsync(); */ }
  void DtbsStore_Chngd(string val) { DtBsNameProp = val;   /* await RefreshReloadAsync(); */ }
  void EmaiStore_Chngd(string val) { EmailOfIProp = val;   /* await RefreshReloadAsync(); */ }
  void LetCStore_Chngd(bool value) { LetDbChgProp = value; /* await RefreshReloadAsync(); */ }
  string _qs = ""; public string SrvrNameProp { get => _qs; set { if (SetProperty(ref _qs, value, true) && value is not null && _loaded) { Bpr.Click(); UsrStgns.SrvrName = value; SrvrStore.Change(value); } } }
  string _dn = ""; public string DtBsNameProp { get => _dn; set { if (SetProperty(ref _dn, value, true) && value is not null && _loaded) { Bpr.Click(); UsrStgns.DtBsName = value; DtBsStore.Change(value); } } }
  string _em = ""; public string EmailOfIProp { get => _em; set { if (SetProperty(ref _em, value, true) && value is not null && _loaded) { Bpr.Click(); UsrStgns.EmailOfI = value; EmaiStore.Change(value); } } }
  bool _aw; public bool LetDbChgProp { get => _aw; set { if (SetProperty(ref _aw, value, true) && _loaded) { Bpr.Click(); UsrStgns.LetDbChg = value; _letStore.Change(value); } } }

  string? _ds; public string DeploymntSrcExe { get => _ds ?? Deployment.DeplSrcExe; set => _ds = value; }
  public IBpr Bpr { get; }
  public ILogger Logger { get; }
  public UserSettings UsrStgns { get; }
  public NavBarVM NavBarVM { get; }
  public BaseMinVM? CurrentVM => _navigationStore.CurrentVM;
  public List<string> SqlServrs { get; } = new();
  public List<string> DtBsNames { get; } = new();


  [ObservableProperty] double upgradeUrgency = 1;         // in days
  [ObservableProperty] string appVerNumber = "0.0";
  [ObservableProperty] object appVerToolTip = "Old";
  [ObservableProperty] string busyMessage = "Loading...";
  [ObservableProperty] string gSReport = "Loading...";
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] bool isObsolete;
  [ObservableProperty] int navAnmDirn;
  [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible;
  [ObservableProperty] Visibility gSRepViz = Visibility.Visible;
  [ObservableProperty] bool isBusy;// /*BusyBlur = value ? 8 : 0;*/  }
  [ObservableProperty] ObservableCollection<string?> validationMessages = new();
  bool _au; public bool IsAudible { get => _au; set { if (SetProperty(ref _au, value) && _ctored) { Bpr.SuppressTicks = Bpr.SuppressAlarm = !(UsrStgns.IsAudible = value); Logger.LogInformation($"│   user-pref-auto-poll:       IsAudible: {value} ■─────■"); } } }
  bool _an; public bool IsAnimeOn { get => _an; set { if (SetProperty(ref _an, value) && _ctored) { UsrStgns.IsAnimeOn = value; Logger.LogInformation($"│   user-pref-auto-poll:       IsAnimeOn: {value} ■─────■"); } } }

  void OnCurrentVMChanged() => OnPropertyChanged(nameof(CurrentVM));
  void OnCurrentModalVMChanged()
  {
    //OnPropertyChanged(nameof(CurrentModalVM));
    //OnPropertyChanged(nameof(IsOpen));
  }

  [RelayCommand] async Task UpgradeSelf() { await Task.Yield(); ; }
  [RelayCommand] async Task HidePnl() { await Task.Yield(); ; }
}