namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
  readonly int _hashCode;
  readonly MainVM _mainVM;
  readonly ISecForcer _secForcer;
  protected bool _saving, _loading, _inited;
  protected readonly DateTime Now = DateTime.Now;
  public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, /*EmailOfIStore eml,*/ LetDbChgStore awd, UserSettings usrStgns, int oid)
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

    SrvrNameStore = svr; SrvrNameStore.Changed += SrvrNameStore_Chngd;
    DtBsNameStore = dbs; DtBsNameStore.Changed += DtBsNameStore_Chngd;
    GSReportStore = gsr; GSReportStore.Changed += GSReportStore_Chngd;
    _letStore = awd; _letStore.Changed += LetDbChgStore_Chngd;

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
  public override async Task<bool> WrapAsync()
  {
    try
    {
      if (LetDbChg && Dbx.HasUnsavedChanges())
      {
        switch (MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
          default:
          case MessageBoxResult.Cancel: return false;
          case MessageBoxResult.Yes: await SaveLogReportOrThrow(Dbx); break;
          case MessageBoxResult.No: Dbx.DiscardChanges(); break;
        }
      }

      SrvrNameStore.Changed -= SrvrNameStore_Chngd;
      DtBsNameStore.Changed -= DtBsNameStore_Chngd;
      GSReportStore.Changed -= GSReportStore_Chngd;
      _letStore.Changed -= LetDbChgStore_Chngd;

      //PopupMsg(GSReport = "");

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
    SrvrNameStore.Changed -= SrvrNameStore_Chngd;
    DtBsNameStore.Changed -= DtBsNameStore_Chngd;
    GSReportStore.Changed -= GSReportStore_Chngd;
    _letStore.Changed -= LetDbChgStore_Chngd;

    base.Dispose();
  }
  public virtual async Task RefreshReloadAsync([CallerMemberName] string? cmn = "") { WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() "); await Task.Yield(); }
  protected void ReportProgress(string msg) { GSReport = msg; Lgr.Log(LogLevel.Trace, msg); }

  protected readonly LetDbChgStore _letStore;
  public SrvrNameStore SrvrNameStore { get; }
  public DtBsNameStore DtBsNameStore { get; }
  public GSReportStore GSReportStore { get; }
  async void SrvrNameStore_Chngd(string val) { SrvrName = val; await RefreshReloadAsync(); }
  async void DtBsNameStore_Chngd(string val) { DtBsName = val; await RefreshReloadAsync(); }
  async void GSReportStore_Chngd(string val) { GSReport = val; await RefreshReloadAsync(); }
  async void LetDbChgStore_Chngd(bool value) { LetDbChg = value; await RefreshReloadAsync(); }
  string? _cs; public string? SrvrName
  {
    get => _cs; set
    {
      if (SetProperty(ref _cs, value) && value is not null && _inited)
      {
        Bpr.Click();
        UsrStgns.SrvrName = value;
        SrvrNameStore.Change(value);
      }
    }
  }
  string? _cd; public string? DtBsName
  {
    get => _cd; set
    {
      if (SetProperty(ref _cd, value) && value is not null && _inited)
      {
        Bpr.Click();
        UsrStgns.DtBsName = value;
        DtBsNameStore.Change(value);
      }
    }
  }
  string? _gr; public string? GSReport
  {
    get => _gr; set
    {
      if (SetProperty(ref _gr, value) && value is not null && _inited)
      {
        Bpr.Click();
        GSReportStore.Change(value);
      }
    }
  }
  bool _aw; public bool LetDbChg { get => _aw; set { if (SetProperty(ref _aw, value)) { _letStore.Change(value); } } }
  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }

  public UserSettings UsrStgns { get; }
  public IConfigurationRoot Cfg { get; }
  public QstatsRlsContext Dbx { get; }
  public ILogger Lgr { get; }
  public IBpr Bpr { get; }
  public Window MainWin { get; }
  [ObservableProperty] bool isDevDbg;
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
  string _f = ""; public string SearchText { get => _f; set { if (SetProperty(ref _f, value)) { Bpr.Tick(); PageCvs?.Refresh(); } } }
  bool _ic; public bool IncludeClosed { get => _ic; set { if (SetProperty(ref _ic, value)) { Bpr.Tick(); PageCvs?.Refresh(); } } }
  bool _hc; public bool HasChanges { get => _hc; set { if (SetProperty(ref _hc, value)) Save2DbCommand.NotifyCanExecuteChanged(); } }
  
  async Task<string> SaveLogReportOrThrow(DbContext dbx, string note = "", [CallerMemberName] string? cmn = "")
  {
    if (LetDbChg)
    {
      var (success, rowsSaved, report) = await dbx.TrySaveReportAsync($" {nameof(SaveLogReportOrThrow)} called by {cmn} on {dbx.GetType().Name}.  {note}");
      if (!success) throw new Exception(report);

      Lgr.LogInformation(report);
      GSReport = report;
    }
    else
    {
      GSReport = $"Current user permisssion \n\n    {_secForcer.PermisssionCSV} \n\ndoes not include database modifications.";
      Lgr.LogWarning(GSReport.Replace("\n", "") + "▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ");
      await Bpr.BeepAsync(333, 2.5); // _ = MessageBox.Show(report, $"Not enough priviliges \t\t {DateTime.Now:MMM-dd HH:mm}", MessageBoxButton.OK, MessageBoxImage.Hand);
    }

    return GSReport;
  }

  [RelayCommand] protected void ChkDb4Cngs() { Bpr.Click(); GSReport = Dbx.GetDbChangesReport(); HasChanges = Dbx.HasUnsavedChanges();  WriteLine(GSReport);  }
  [RelayCommand] protected async Task Save2Db() { try { Bpr.Click(); IsBusy = _saving = true; _ = await SaveLogReportOrThrow(Dbx); } catch (Exception ex) { IsBusy = false; ex.Pop(Lgr); } finally { IsBusy = _saving = false; Bpr.Tick(); } }
}