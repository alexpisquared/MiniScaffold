namespace MinNavTpl.VM.VMs;
public partial class MainVM : BaseMinVM
{
  readonly bool _ctored;
  readonly NavigationStore _navigationStore;
  readonly Window _mainWin;
  public MainVM(NavBarVM navBarVM, /*BaseMinVM contentVM,*/ NavigationStore navigationStore, ILogger lgr, IBpr bpr, IConfigurationRoot cfg, UserSettings usrStgns, IAddChild wnd)
  {
    NavBarVM = navBarVM;
    //ContentVM = contentVM;
    _navigationStore = navigationStore;
    Logger = lgr;
    Bpr = bpr;
    UsrStgns = usrStgns;
    _mainWin = (Window)wnd;

    IsDevDbg = VersionHelper.IsDbg;

    _navigationStore.CurrentVMChanged += OnCurrentVMChanged;

    cfg[CfgName.ServerLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => SqlServers.Add(r));
    cfg[CfgName.DtBsNmLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => DtBsNames.Add(r));

    Bpr.SuppressTicks = Bpr.SuppressAlarm = !(IsAudible = UsrStgns.IsAudible);
    IsAnimeOn = UsrStgns.IsAnimeOn;

    Bpr.Start();

    _ctored = true;
  }
  public override async Task<bool> InitAsync()
  {
    SqlServer = UsrStgns.PrefSrvrName;
    DtBsName = UsrStgns.PrefDtBsName;

    AppVerNumber = VersionHelper.CurVerStr("0.M.d");
    AppVerToolTip = VersionHelper.CurVerStr("0.M.d.H.m");

    try { await KeepCheckingForUpdatesAndNeverReturn(); } catch (Exception ex) { ex.Pop(Logger); }

    return await base.InitAsync();
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

  string? _ds; public string DeploymntSrcExe { get => _ds ?? Deployment.DeplSrcExe; set => _ds = value; }
  public IBpr Bpr { get; }
  public ILogger Logger { get; }
  public UserSettings UsrStgns { get; }
  public NavBarVM NavBarVM { get; }
  //public BaseMinVM ContentVM { get; } // not used here (see LayoutViewModel.xs)
  public BaseMinVM? CurrentVM => _navigationStore.CurrentVM;
  public List<string> SqlServers { get; } = new();
  public List<string> DtBsNames { get; } = new();
  string _qs = default!; public string SqlServer
  {
    get => _qs; set
    {
      if (SetProperty(ref _qs, value, true) && value is not null && _loaded)
      {
        Bpr.Click();

        UsrStgns.PrefSrvrName = value;

        _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
        _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
        {
          await Task.Delay(2600);
          Application.Current.Shutdown();
        });
      }
    }
  }
  string _dn = default!; public string DtBsName
  {
    get => _dn; set
    {
      if (SetProperty(ref _dn, value, true) && value is not null && _loaded)
      {
        Bpr.Click();          

        UsrStgns.PrefSrvrName = value;

        _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
        _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
        {
          await Task.Delay(2600);
          Application.Current.Shutdown();
        });
      }
    }
  }

  [ObservableProperty] double upgradeUrgency = 1;         // in days
  [ObservableProperty] string appVerNumber = "0.0";
  [ObservableProperty] object appVerToolTip = "Old";
  [ObservableProperty] string busyMessage = "Loading...";
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] bool isObsolete;
  [ObservableProperty] Visibility isDevDbgViz = Visibility.Visible; 
  bool _ib; public bool IsBusy { get => _ib; set { if (SetProperty(ref _ib, value)) { Write($"TrcW:>         ├──   MainVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n"); } } /*BusyBlur = value ? 8 : 0;*/  }
  bool _au; public bool IsAudible
  {
    get => _au; set
    {
      if (SetProperty(ref _au, value) && _ctored)
      {
        Bpr.SuppressTicks = Bpr.SuppressAlarm = !(UsrStgns.IsAudible = value);
        Logger.LogInformation($"│   user-pref-auto-poll:       IsAudible: {value} ■─────■");
      }
    }
  }
  bool _an; public bool IsAnimeOn
  {
    get => _an; set
    {
      if (SetProperty(ref _an, value) && _ctored)
      {
        UsrStgns.IsAnimeOn = value;
        Logger.LogInformation($"│   user-pref-auto-poll:       IsAnimeOn: {value} ■─────■");
      }
    }
  }

  void OnCurrentVMChanged() => OnPropertyChanged(nameof(CurrentVM));
  void OnCurrentModalVMChanged()
  {
    //OnPropertyChanged(nameof(CurrentModalVM));
    //OnPropertyChanged(nameof(IsOpen));
  }

  IRelayCommand? _up; public IRelayCommand UpgradeSelfCmd => _up ??= new AsyncRelayCommand(PerformUpgradeSelf); async Task PerformUpgradeSelf()
  {
    try
    {
      IsBusy = true;
      BusyMessage = "Copying...";
      IsObsolete = false; // hide the clicked button lest user double-clicked on it.

      var p = new Process
      {
        StartInfo = new ProcessStartInfo()
        {
          FileName = DeploymntSrcExe,
          Arguments = $"{new WindowInteropHelper(_mainWin).Handle} {Deployment.DeplSrcDir} {Deployment.DeplTrgDir} {Deployment.DeplTrgExe}",
          UseShellExecute = true
        }
      };

      Logger.LogInformation($"│   PerformUpgradeSelf() launched with args:  '{p.StartInfo.Arguments}'  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ■  ");

      Application.Current.MainWindow.WindowState = WindowState.Minimized;  // apparently Application.Current.MainWindow works better than _mainWin.
      //Application.Current.MainWindow.Hide();                             // apparently Application.Current.MainWindow works better than _mainWin.
      _ = p.Start();
      Application.Current.MainWindow.WindowState = WindowState.Normal;     // apparently Application.Current.MainWindow works better than _mainWin.
      await Task.Delay(20000);                       // keep it around for better user experience: at least Wait is shown, as opposed to abrupt dissapearance of the app.

      Application.Current.Shutdown();
    }
    catch (Exception ex)
    {
      ex.Pop(Logger);
    }
    finally
    {
      Application.Current.MainWindow.WindowState = WindowState.Normal;      // apparently Application.Current.MainWindow works better than _mainWin.
      Application.Current.MainWindow.Show();                                // apparently Application.Current.MainWindow works better than _mainWin.
      IsBusy = false;
      IsObsolete = true;
    }
  }
  
  public override void Dispose()
  {
    NavBarVM.Dispose();
    //ContentVM.Dispose();

    base.Dispose();
  }
}