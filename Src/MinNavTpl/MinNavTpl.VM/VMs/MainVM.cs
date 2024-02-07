namespace MinNavTpl.VM.VMs;
public partial class MainVM : BaseMinVM
{
    readonly bool _ctored;
    readonly NavigationStore _navigationStore;
    public MainVM(NavBarVM navBarVM, NavigationStore navigationStore, ILogger lgr, IBpr bpr, IConfigurationRoot cfg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore alw, UserSettings usr) : base()
    {
        NavBarVM = navBarVM;
        _navigationStore = navigationStore;
        Logger = lgr;
        Bpr = bpr;
        UsrStgns = usr;

        IsDevDbg = VersionHelper.IsDbg;

        _navigationStore.CurrentVMChanged += OnCurrentVMChanged;

        SrvrNameStore = svr; SrvrNameStore.Changed += SrvrNameStore_Chngd;
        DtBsNameStore = dbs; DtBsNameStore.Changed += DtBsNameStore_Chngd;
        GSReportStore = gsr; GSReportStore.Changed += GSReportStore_Chngd;
        EmailOfIStore = eml; EmailOfIStore.Changed += EmailOfIStore_Chngd;
        _letDbChStore = alw; _letDbChStore.Changed += LetDbChgStore_Chngd;

        cfg[CfgName.ServerLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(SqlServrs.Add);
        cfg[CfgName.DtBsNmLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(DtBsNames.Add);

        Bpr.SuppressTicks = Bpr.SuppressAlarm = !(IsAudible = UsrStgns.IsAudible);
        IsAnimeOn = UsrStgns.IsAnimeOn;

        _ctored = true;
    }
    public async override Task<bool> InitAsync()
    {
        SrvrName = UsrStgns.SrvrName;
        DtBsName = UsrStgns.DtBsName;
        EmailOfI = UsrStgns.EmailOfI;
        LetDbChg = UsrStgns.LetDbChg;

        AppVerNumber = VersionHelper.CurVerStr;// fmt("0.M.d");
        AppVerToolTip = VersionHelper.CurVerStr;// YYMMDDHHmm;// CurVerStr("0.M.d.H.m");

        var rv = await base.InitAsync();

        try { await KeepCheckingForUpdatesAndNeverReturnAsync(); } catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Logger); }

        return rv;
    }
    public override void Dispose()
    {
        NavBarVM.Dispose();

        _navigationStore.CurrentVMChanged -= OnCurrentVMChanged;

        SrvrNameStore.Changed -= SrvrNameStore_Chngd;
        DtBsNameStore.Changed -= DtBsNameStore_Chngd;
        GSReportStore.Changed -= GSReportStore_Chngd;
        EmailOfIStore.Changed -= EmailOfIStore_Chngd;
        _letDbChStore.Changed -= LetDbChgStore_Chngd;

        base.Dispose();
    }
    async Task KeepCheckingForUpdatesAndNeverReturnAsync()
    {
        await Task.Delay(3_600_000); // 1 hr
        OnCheckForNewVersion();

        var nextHour = DateTime.Now.AddHours(1);
        var nextHH00 = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 5);
        await Task.Delay(nextHH00 - DateTime.Now);
        OnCheckForNewVersion(true);

        while (await new PeriodicTimer(TimeSpan.FromHours(1)).WaitForNextTickAsync()) { OnCheckForNewVersion(); }
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
            AppVerToolTip = IsObsolete ? $" New version is available:   0.{setupExeTime:M.d.HHmm} \n\t         from  {setupExeTime:yyyy-MM-dd HH:mm}.\n Click to update. " : $" This is the latest version  {VersionHelper.CurVerStr} \n\t               from  {VersionHelper.TimedVer:yyyy-MM-dd HH:mm}. ";
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; Logger.LogError(ex, "│   ▄─▀─▄─▀─▄ -- Ignore"); }
    }

    protected readonly LetDbChgStore _letDbChStore;
    public SrvrNameStore SrvrNameStore
    {
        get;
    }
    public DtBsNameStore DtBsNameStore
    {
        get;
    }
    public GSReportStore GSReportStore
    {
        get;
    }
    public EmailOfIStore EmailOfIStore
    {
        get;
    }
    void SrvrNameStore_Chngd(string val) => SrvrName = val;   /* await RefreshReloadAsync(); */
    void DtBsNameStore_Chngd(string val) => DtBsName = val;   /* await RefreshReloadAsync(); */
    void GSReportStore_Chngd(string val)
    {
        if (!GSReport.EndsWith(val)) GSReport += val; /*await RefreshReloadAsync();*/
    }
    void EmailOfIStore_Chngd(string emailOfI, [CallerMemberName] string? cmn = "")
    {
        WriteLine($"■■ MAIN  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22}  {(EmailOfI != emailOfI ? "==>Load as it were ..." : "==>----")}");
        EmailOfI = emailOfI;   /* await RefreshReloadAsync(); */
    }
    void LetDbChgStore_Chngd(bool value) => LetDbChg = value; /* await RefreshReloadAsync(); */
    string _qs = ""; public string SrvrName
    {
        get => _qs; set
        {
            if (SetProperty(ref _qs, value, true) && value is not null && _loaded) { Bpr.Tick(); SrvrNameStore.Change(value); UsrStgns.SrvrName = value; }
        }
    }
    string _dn = ""; public string DtBsName
    {
        get => _dn; set
        {
            if (SetProperty(ref _dn, value, true) && value is not null && _loaded) { Bpr.Tick(); DtBsNameStore.Change(value); UsrStgns.DtBsName = value; }
        }
    }
    string _gr = ""; public string GSReport
    {
        get => _gr; set
        {
            if (SetProperty(ref _gr, value, true) && value is not null && _loaded) { /*       */ GSReportStore.Change(value); GSRepViz = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible; }
        }
    }
    string _em = ""; public string EmailOfI
    {
        get => _em; set
        {
            if (SetProperty(ref _em, value, true) && value is not null && _loaded) { Bpr.Tick(); EmailOfIStore.Change(value); UsrStgns.EmailOfI = value; }
        }
    }
    bool _aw; public bool LetDbChg
    {
        get => _aw; set
        {
            if (SetProperty(ref _aw, value, true) && _loaded) { Bpr.Tick(); UsrStgns.LetDbChg = value; _letDbChStore.Change(value); }
        }
    }

    string? _ds; public string DeploymntSrcExe
    {
        get => _ds ?? "??Deployment.DeplSrcExe??"; set => _ds = value;
    }
    public IBpr Bpr
    {
        get;
    }
    public ILogger Logger
    {
        get;
    }
    public UserSettings UsrStgns
    {
        get;
    }
    public NavBarVM NavBarVM
    {
        get;
    }
    public BaseMinVM? CurrentVM => _navigationStore.CurrentVM;
    public List<string> SqlServrs { get; } = [];
    public List<string> DtBsNames { get; } = [];

    [ObservableProperty] double upgradeUrgency = 1;         // in days
    [ObservableProperty] string appVerNumber = "0.0";
    [ObservableProperty] object appVerToolTip = "Old";
    [ObservableProperty] string busyMessage = "Loading...";
    [ObservableProperty] bool isDevDbg;
    [ObservableProperty] bool isObsolete;
    [ObservableProperty] int navAnmDirn;
    [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible;
    [ObservableProperty] Visibility gSRepViz = Visibility.Visible;
    [ObservableProperty] bool isBusy;// /*BusyBlur = value ? 8 : 0;*/  }
    [ObservableProperty] ObservableCollection<string?> validationMessages = [];
    bool _au; public bool IsAudible
    {
        get => _au; set
        {
            if (SetProperty(ref _au, value) && _ctored) { Bpr.SuppressTicks = Bpr.SuppressAlarm = !(UsrStgns.IsAudible = value); Logger.LogInformation($"│   user-pref-auto-poll:       IsAudible: {value} ■─────■"); }
        }
    }
    bool _an; public bool IsAnimeOn
    {
        get => _an; set
        {
            if (SetProperty(ref _an, value) && _ctored) { UsrStgns.IsAnimeOn = value; Logger.LogInformation($"│   user-pref-auto-poll:       IsAnimeOn: {value} ■─────■"); }
        }
    }

    void OnCurrentVMChanged() => OnPropertyChanged(nameof(CurrentVM));
    void OnCurrentModalVMChanged()
    {
        //OnPropertyChanged(nameof(CurrentModalVM));
        //OnPropertyChanged(nameof(IsOpen));
    }

    [RelayCommand] async Task UpgradeSelfAsync() => await Task.Yield();

    [RelayCommand]
    void HidePnl()
    {
        GSReport = ""; GSRepViz = Visibility.Collapsed;
    }

    [RelayCommand]
    async Task TryCloseAsync()
    {
        var vm = CurrentVM as LayoutVM;
        var isReadyToClose = vm is not null && await vm.ContentVM.WrapAsync();
        if (isReadyToClose)
        {
            Application.Current.Shutdown();
        }
    }
}