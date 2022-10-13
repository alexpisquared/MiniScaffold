using MinNavTpl.VM.Stores;

namespace MinNavTpl.VM.VMs;
public partial class MainVM : BaseMinVM
{
  readonly bool _ctored;
  readonly NavigationStore _navigationStore;
  readonly Window _mainWin;
  public MainVM(NavigationStore navigationStore, ILogger lgr, IBpr bpr, UserSettings usrStgns, IAddChild wnd)
  {
    _navigationStore = navigationStore;
    Logger = lgr;
    Bpr = bpr;
    UsrStgns = usrStgns;
    _mainWin = (Window)wnd;

    IsDevDbg = VersionHelper.IsDbg;

    _navigationStore.CurrentVMChanged += OnCurrentVMChanged;

    if (DevOps.IsSelectModes)
    {
      UsrStgns.IsAudible = false;
      UsrStgns.IsAnimeOn = false;
    }

    Bpr.SuppressTicks = Bpr.SuppressAlarm = !(IsAudible = UsrStgns.IsAudible);
    IsAnimeOn = UsrStgns.IsAnimeOn;

    Bpr.Start();

    _ctored = true;
  }
  public override async Task<bool> InitAsync()
  {
    AppVerNumber = VersionHelper.CurVerStrYYMMDD;
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
  public BaseMinVM? CurrentVM => _navigationStore.CurrentVM;

  [ObservableProperty] double upgradeUrgency = 1;         // in days
  [ObservableProperty] string appVerNumber = "0.0";       
  [ObservableProperty] object appVerToolTip = "Old";      
  [ObservableProperty] string busyMessage = "Loading..."; 
  [ObservableProperty] bool isDevDbg;                     
  [ObservableProperty] bool isObsolete;                   
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
}