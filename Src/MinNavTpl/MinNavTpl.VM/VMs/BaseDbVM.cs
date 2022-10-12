using MinNavTpl.VM.Stores;

namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
  readonly int _hashCode;
  readonly MainVM _mainVM;
  readonly ISecForcer _secForcer;
  protected bool _saving, _loading, _inited;
  protected readonly AllowWriteDBStore _allowWriteDBStore;

  public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, AllowWriteDBStore allowWriteDBStore, UserSettings usrStgns, int oid)
  {
    IsDevDbg = VersionHelper.IsDbg;

    _mainVM = mainVM;
    Logger = lgr;
    Config = cfg;
    DbxInv = inv;
    Bpr = bpr;
    _secForcer = sec;
    MainWin = (Window)win;
    _hashCode = GetType().GetHashCode();
    UserSetgs = usrStgns;

    _awd = IsDevDbg && _secForcer.CanEdit && (
      UserSetgs.PrefSrvrName is null ? false :
      UserSetgs.PrefSrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UserSetgs.AllowWriteDB);

    _allowWriteDBStore = allowWriteDBStore; _allowWriteDBStore.AllowWriteDBChanged += AllowWriteDBStore_AllowWriteDBChanged;

    Logger.LogInformation($"┌── {GetType().Name} eo-ctor      PageRank:{oid}");
  }

  public override async Task<bool> InitAsync()
  {
    _inited = true;
    Logger.LogInformation($"├── {GetType().Name} eo-init     _hash:{_hashCode,-10}   br.hash:{DbxInv.GetType().GetHashCode(),-10}");
    await Bpr.FinishAsync();
    return true;
  }
  public virtual async Task VMSpecificSaveToDB(object? isGoingBack) => await SaveLogReportOrThrow(DbxInv);
  public override async Task<bool> WrapAsync()
  {
    try
    {
      if (AllowWriteDB && DbxInv.HasUnsavedChanges())
      {
        switch (MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
          default:
          case MessageBoxResult.Cancel: return false;
          case MessageBoxResult.Yes: await VMSpecificSaveToDB($" ..from {nameof(BaseDbVM)}.{nameof(WrapAsync)}() "); break;
          case MessageBoxResult.No: DbxInv.DiscardChanges(); break;
        }
      }

      //PopupMsg(Report = "");
      return true;
    }
    catch (Exception ex) { IsBusy = false; ex.Pop(Logger); return false; }
    finally
    {
      Logger.LogInformation($"└── {GetType().Name} eo-wrap     _hash:{_hashCode,-10}   br.hash:{DbxInv.GetType().GetHashCode(),-10}  ");
    }
  }
  public virtual async Task RefreshReloadAsync([CallerMemberName] string? cmn = "") { WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() "); await Task.Yield(); }

  void AllowWriteDBStore_AllowWriteDBChanged(bool val) { AllowWriteDB = val; ; }

  public UserSettings UserSetgs { get; }
  public IConfigurationRoot Config { get; }
  public InventoryContext DbxInv { get; }
  public ILogger Logger { get; }
  public IBpr Bpr { get; }
  public Window MainWin { get; }

  bool _awd; public bool AllowWriteDB
  {
    get => _awd; set
    {
      if (SetProperty(ref _awd, value))
      {
        //?UserSetgs.AllowWriteDB = value;
        _allowWriteDBStore.ChangAllowWriteDB(value);
      }
    }
  }

  [ObservableProperty] bool isDevDbg;       //public bool IsDevDbg { get => isDevDbg; set => SetProperty(ref isDevDbg, value); }
  [ObservableProperty] string report = "";  //public string Report { get => report; set => SetProperty(ref report, value); }
  bool _ib; public bool IsBusy { get => _ib; set { if (SetProperty(ref _ib, value)) { Write($"TrcW:>         ├── BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n"); _mainVM.IsBusy = value; } } /*BusyBlur = value ? 8 : 0;*/  }
  bool _hc; public bool HasChanges { get => _hc; set { if (SetProperty(ref _hc, value)) Save2DbCommand.NotifyCanExecuteChanged(); } }

  [RelayCommand] void CheckDb() { Bpr.Click(); Report = DbxInv.GetDbChangesReport(); HasChanges = DbxInv.HasUnsavedChanges(); }
  [RelayCommand] async Task Save2Db()
  {
    try
    {
      IsBusy = _saving = true;
      Report = await SaveLogReportOrThrow(DbxInv);
    }
    catch (Exception ex) { IsBusy = false; ex.Pop(Logger); }
    finally { IsBusy = _saving = false; Bpr.Tick(); }
  }

  public async Task<string> SaveLogReportOrThrow(DbContext dbContext, string note = "", [CallerMemberName] string? cmn = "")
  {
    if (AllowWriteDB)
    {
      var (success, rowsSaved, report) = await dbContext.TrySaveReportAsync($" {nameof(SaveLogReportOrThrow)} called by {cmn} on {dbContext.GetType().Name}.  {note}");
      if (!success) throw new Exception(report);

      Logger.LogInformation(report);
      Report = $"{rowsSaved} rows saved.";

      return report;
    }
    else
    {
      var report = $"Current user permisssion \n\n    {_secForcer.PermisssionCSV} \n\ndoes not include database modifications.";
      Logger.LogWarning(report.Replace("\n", "") + "▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ");
      await Bpr.BeepAsync(333, 2.5); // _ = MessageBox.Show(report, $"Not enough priviliges \t\t {DateTime.Now:MMM-dd HH:mm}", MessageBoxButton.OK, MessageBoxImage.Hand);
      return report;
    }
  }
  protected string? GetCaller([CallerMemberName] string? cmn = "") => cmn;
}