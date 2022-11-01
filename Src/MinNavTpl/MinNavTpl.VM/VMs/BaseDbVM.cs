using MinNavTpl.Stores;

namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
  readonly int _hashCode;
  readonly MainVM _mainVM;
  readonly ISecForcer _secForcer;
  protected bool _saving, _loading, _inited;
  protected readonly LetDbChgStore _allowWriteDBStore;

  public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, SrvrNameStore srvrStore, DtBsNameStore dtbsStore, LetDbChgStore allowWriteDBStore, UserSettings usrStgns, int oid)
  {
    IsDevDbg = VersionHelper.IsDbg;

    Lgr = lgr;
    Cfg = cfg;
    Dbx = dbx;
    Bpr = bpr;
    MainWin = (Window)win;
    UsrStgns = usrStgns;
    _mainVM = mainVM;
    _secForcer = sec;
    _hashCode = GetType().GetHashCode();

    _aw = IsDevDbg && _secForcer.CanEdit && (
      UsrStgns.PrefSrvrName is null ? false :
      UsrStgns.PrefSrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UsrStgns.AllowWriteDB);

    _allowWriteDBStore = allowWriteDBStore; _allowWriteDBStore.AllowWriteDBChanged += AllowWriteDBStore_Changed;

    SrvrStore = srvrStore; SrvrStore.CurrentSrvrChanged += SrvrStore_Chngd;
    DtBsStore = dtbsStore; DtBsStore.CurrentDtbsChanged += DtbsStore_Chngd;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

    Lgr.LogInformation($"┌── {GetType().Name} eo-ctor      PageRank:{oid}");
  }

  public override async Task<bool> InitAsync()
  {
    IsBusy = false;
    _inited = true;
    Lgr.LogInformation($"├── {GetType().Name} eo-init     _hash:{_hashCode,-10}   br.hash:{Dbx.GetType().GetHashCode(),-10}");
    await Bpr.FinishAsync();
    return true;
  }
  public virtual async Task VMSpecificSaveToDB(object? isGoingBack) => await SaveLogReportOrThrow(Dbx);
  public override async Task<bool> WrapAsync()
  {
    try
    {
      if (AllowWriteDB && Dbx.HasUnsavedChanges())
      {
        switch (MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
          default:
          case MessageBoxResult.Cancel: return false;
          case MessageBoxResult.Yes: await VMSpecificSaveToDB($" ..from {nameof(BaseDbVM)}.{nameof(WrapAsync)}() "); break;
          case MessageBoxResult.No: Dbx.DiscardChanges(); break;
        }
      }

      SrvrStore.CurrentSrvrChanged -= SrvrStore_Chngd;
      SrvrStore.CurrentSrvrChanged -= SrvrStore_Chngd;

      //PopupMsg(Report = "");
      return true;
    }
    catch (Exception ex) { IsBusy = false; ex.Pop(Lgr); return false; }
    finally
    {
      Lgr.LogInformation($"└── {GetType().Name} eo-wrap     _hash:{_hashCode,-10}   br.hash:{Dbx.GetType().GetHashCode(),-10}  ");
    }
  }
  public override void Dispose()
  {
    SrvrStore.CurrentSrvrChanged -= SrvrStore_Chngd;
    DtBsStore.CurrentDtbsChanged -= DtbsStore_Chngd;

    base.Dispose();
  }

  public virtual async Task RefreshReloadAsync([CallerMemberName] string? cmn = "") { WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() "); await Task.Yield(); }




  public SrvrNameStore SrvrStore { get; }
  public DtBsNameStore DtBsStore { get; }
  async void SrvrStore_Chngd(ADSrvr srvr) { SelectSrvr = srvr; await RefreshReloadAsync(); }
  async void DtbsStore_Chngd(ADDtBs srvr) { SelectDtBs = srvr; await RefreshReloadAsync(); }
  void AllowWriteDBStore_Changed(bool val) { AllowWriteDB = val; ; }
  ADSrvr? _cs; public ADSrvr? SelectSrvr
  {
    get => _cs; set
    {
      if (SetProperty(ref _cs, value) && value is not null && _inited)
      {
        try
        {
          Bpr.Click();          //ArgumentNullException.ThrowIfNull(value, nameof(value));
          UsrStgns.PrefSrvrName = value.Name;
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
        catch (Exception ex) { ex.Pop(Lgr); }
      }
    }
  }
  ADDtBs? _cd; public ADDtBs? SelectDtBs
  {
    get => _cd; set
    {
      if (SetProperty(ref _cd, value) && value is not null && _inited)
      {
        Bpr.Click();
        UsrStgns.PrefDtBsName = value.Name;
        DtBsStore.ChgDtBs(value);
        //Page01VMHelpers.LoadDtBsRolesFAF(value.Name, RoleList, _spm, string.Format(Cfg[GenConst.SqlVerSpm] ?? "'", SelectSrvr?.Name, SelectDtBs?.Name), Lgr);
      }
    }
  }
  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }


  public UserSettings UsrStgns { get; }
  public IConfigurationRoot Cfg { get; }
  public QStatsRlsContext Dbx { get; }
  public ILogger Lgr { get; }
  public IBpr Bpr { get; }
  public Window MainWin { get; }
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] string report = "";
  bool _ib; public bool IsBusy { get => _ib; set { if (SetProperty(ref _ib, value)) { Write($"TrcW:>         ├── BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n"); _mainVM.IsBusy = value; } } /*BusyBlur = value ? 8 : 0;*/  }
  bool _hc; public bool HasChanges { get => _hc; set { if (SetProperty(ref _hc, value)) Save2DbCommand.NotifyCanExecuteChanged(); } }
  bool _aw; public bool AllowWriteDB { get => _aw; set { if (SetProperty(ref _aw, value)) { _allowWriteDBStore.ChangAllowWriteDB(value); } } }

  [RelayCommand] void CheckDb() { Bpr.Click(); Report = Dbx.GetDbChangesReport(); HasChanges = Dbx.HasUnsavedChanges(); }
  [RelayCommand]
  async Task Save2Db()
  {
    try
    {
      Bpr.Click();
      IsBusy = _saving = true;
      _ = await SaveLogReportOrThrow(Dbx);
    }
    catch (Exception ex) { IsBusy = false; ex.Pop(Lgr); }
    finally { IsBusy = _saving = false; Bpr.Tick(); }
  }

  public async Task<string> SaveLogReportOrThrow(DbContext dbx, string note = "", [CallerMemberName] string? cmn = "")
  {
    if (AllowWriteDB)
    {
      var (success, rowsSaved, report) = await dbx.TrySaveReportAsync($" {nameof(SaveLogReportOrThrow)} called by {cmn} on {dbx.GetType().Name}.  {note}");
      if (!success) throw new Exception(report);

      Lgr.LogInformation(report);
      Report = report;
    }
    else
    {
      Report = $"Current user permisssion \n\n    {_secForcer.PermisssionCSV} \n\ndoes not include database modifications.";
      Lgr.LogWarning(Report.Replace("\n", "") + "▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ");
      await Bpr.BeepAsync(333, 2.5); // _ = MessageBox.Show(report, $"Not enough priviliges \t\t {DateTime.Now:MMM-dd HH:mm}", MessageBoxButton.OK, MessageBoxImage.Hand);
    }

    return Report;
  }
  protected string? GetCaller([CallerMemberName] string? cmn = "") => cmn;
}