namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
  readonly int _hashCode;
  readonly MainVM _mainVM;
  readonly ISecForcer _secForcer;
  protected bool _saving, _loading, _inited;
  protected readonly DateTime Now = DateTime.Now;
  public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, SrvrNameStore srvrStore, DtBsNameStore dtbsStore, LetDbChgStore letDStore, UserSettings usrStgns, int oid)
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
      UsrStgns.SrvrName is null ? false :
      UsrStgns.SrvrName.Contains("PRD", StringComparison.OrdinalIgnoreCase) ? false :
      UsrStgns.LetDbChg);

    SrvrStore = srvrStore; SrvrStore.Changed += SrvrStore_Chngd;
    DtBsStore = dtbsStore; DtBsStore.Changed += DtbsStore_Chngd;
    _letStore = letDStore; _letStore.Changed += LetCStore_Chngd;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

    Lgr.LogInformation($"┌── {GetType().Name} eo-ctor      PageRank:{oid}");
  }
  public override async Task<bool> InitAsync()
  {
    IsBusy = false;
    _inited = true;
    //Lgr.LogInformation($"├── {GetType().Name} eo-init     _hash:{_hashCode,-10}   br.hash:{Dbx.GetType().GetHashCode(),-10}");
    Bpr.Finish();
    return await base.InitAsync();
  }
  public virtual async Task VMSpecificSaveToDB(object? isGoingBack) => await SaveLogReportOrThrow(Dbx);
  public override async Task<bool> WrapAsync()
  {
    try
    {
      if (LetDbChgProp && Dbx.HasUnsavedChanges())
      {
        switch (MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
          default:
          case MessageBoxResult.Cancel: return false;
          case MessageBoxResult.Yes: await VMSpecificSaveToDB($" ..from {nameof(BaseDbVM)}.{nameof(WrapAsync)}() "); break;
          case MessageBoxResult.No: Dbx.DiscardChanges(); break;
        }
      }

      SrvrStore.Changed -= SrvrStore_Chngd;
      DtBsStore.Changed -= DtbsStore_Chngd;
      _letStore.Changed -= LetCStore_Chngd;

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
    SrvrStore.Changed -= SrvrStore_Chngd;
    DtBsStore.Changed -= DtbsStore_Chngd;
    _letStore.Changed -= LetCStore_Chngd;

    base.Dispose();
  }
  public virtual async Task RefreshReloadAsync([CallerMemberName] string? cmn = "") { WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() "); await Task.Yield(); }
  protected void ReportProgress(string msg) { Report = msg; Lgr.Log(LogLevel.Trace, msg); }

  protected readonly LetDbChgStore _letStore;
  public SrvrNameStore SrvrStore { get; }
  public DtBsNameStore DtBsStore { get; }
  async void SrvrStore_Chngd(string val) { SrvrNameProp = val; await RefreshReloadAsync(); }
  async void DtbsStore_Chngd(string val) { DtBsNameProp = val; await RefreshReloadAsync(); }
  async void LetCStore_Chngd(bool value) { LetDbChgProp = value; await RefreshReloadAsync(); }
  string? _cs; public string? SrvrNameProp
  {
    get => _cs; set
    {
      if (SetProperty(ref _cs, value) && value is not null && _inited)
      {
        Bpr.Click();
        UsrStgns.SrvrName = value;
        SrvrStore.Change(value);
      }
    }
  }
  string? _cd; public string? DtBsNameProp
  {
    get => _cd; set
    {
      if (SetProperty(ref _cd, value) && value is not null && _inited)
      {
        Bpr.Click();
        UsrStgns.DtBsName = value;
        DtBsStore.Change(value);
      }
    }
  }
  bool _aw; public bool LetDbChgProp { get => _aw; set { if (SetProperty(ref _aw, value)) { _letStore.Change(value); } } }
  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }

  public UserSettings UsrStgns { get; }
  public IConfigurationRoot Cfg { get; }
  public QStatsRlsContext Dbx { get; }
  public ILogger Lgr { get; }
  public IBpr Bpr { get; }
  public Window MainWin { get; }
  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] string report = "";
  [ObservableProperty] ICollectionView? pageCvs;

  bool _ib; public bool IsBusy
  {
    get => _ib; set
    {
      if (SetProperty(ref _ib, value))
      {
        //Write($"TrcW:>         ├── BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n");
        _mainVM.IsBusy = value;
      }
    } /*BusyBlur = value ? 8 : 0;*/
  }
  bool _hc; public bool HasChanges { get => _hc; set { if (SetProperty(ref _hc, value)) Save2DbCommand.NotifyCanExecuteChanged(); } }
  string _f = ""; public string SearchText { get => _f; set { if (SetProperty(ref _f, value)) { Bpr.Tick(); PageCvs?.Refresh(); } } }
  bool? _ic; public bool? IncludeClosed { get => _ic; set { if (SetProperty(ref _ic, value)) { Bpr.Tick(); PageCvs?.Refresh(); } } }

  [RelayCommand] protected void ChkDb4Cngs() { Bpr.Click(); Report = Dbx.GetDbChangesReport(); HasChanges = Dbx.HasUnsavedChanges(); }
  [RelayCommand] protected async Task Save2Db() { try { Bpr.Click(); IsBusy = _saving = true; _ = await SaveLogReportOrThrow(Dbx); } catch (Exception ex) { IsBusy = false; ex.Pop(Lgr); } finally { IsBusy = _saving = false; Bpr.Tick(); } }
  async Task<string> SaveLogReportOrThrow(DbContext dbx, string note = "", [CallerMemberName] string? cmn = "")
  {
    if (LetDbChgProp)
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
}