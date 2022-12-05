namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
  readonly int _hashCode;
  readonly MainVM _mainVM;
  readonly ISecurityForcer _secForcer;
  protected bool _saving, _loading, _inited;
  protected readonly DateTime Now = DateTime.Now;
  public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbx, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, /*EmailOfIStore eml,*/ LetDbChgStore awd, UserSettings usrStgns, int oid)
  {
    IsDevDbg = VersionHelper.IsDbg;

    searchText = "";

    Lgr = lgr;
    Cfg = cfg;
    Dbx = dbx;
    Bpr = bpr;
    MainWin = (Window)win;
    UsrStgns = usrStgns;
    _mainVM = mainVM;
    _secForcer = sec;
    _hashCode = GetType().GetHashCode();

    letDbChg = UsrStgns.LetDbChg;

    _SrvrNameStore = svr; _SrvrNameStore.Changed += SrvrNameStore_Chngd;
    _DtBsNameStore = dbs; _DtBsNameStore.Changed += DtBsNameStore_Chngd;
    _GSReportStore = gsr; _GSReportStore.Changed += GSReportStore_Chngd;
    _LetDbChgStore = awd; _LetDbChgStore.Changed += LetDbChgStore_Chngd;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

    Lgr.LogInformation($"┌── {GetType().Name} eo-ctor      PageRank:{oid}");
  }
  public async override Task<bool> InitAsync()
  {
    IsBusy = false;
    _inited = true;
    //Lgr.LogInformation($"├── {GetType().Name} eo-init     _hash:{_hashCode,-10}   br.hash:{Dbx.GetType().GetHashCode(),-10}");
    //too many: Bpr.Finish();
    return await base.InitAsync();
  }
  public async override Task<bool> WrapAsync()
  {
    try
    {
      if (LetDbChg && Dbx.HasUnsavedChanges())
      {
        switch (MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
          default:
          case MessageBoxResult.Cancel: return false;
          case MessageBoxResult.Yes: _ = await SaveLogReportOrThrow(Dbx); break;
          case MessageBoxResult.No: Dbx.DiscardChanges(); break;
        }
      }

      _SrvrNameStore.Changed -= SrvrNameStore_Chngd;
      _DtBsNameStore.Changed -= DtBsNameStore_Chngd;
      _GSReportStore.Changed -= GSReportStore_Chngd;
      _LetDbChgStore.Changed -= LetDbChgStore_Chngd;

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
    _SrvrNameStore.Changed -= SrvrNameStore_Chngd;
    _DtBsNameStore.Changed -= DtBsNameStore_Chngd;
    _GSReportStore.Changed -= GSReportStore_Chngd;
    _LetDbChgStore.Changed -= LetDbChgStore_Chngd;

    base.Dispose();
  }
  public async virtual Task RefreshReloadAsync([CallerMemberName] string? cmn = "") { WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() "); await Task.Yield(); }
  protected void ReportProgress(string msg) { GSReport = msg; Lgr.Log(LogLevel.Trace, msg); }

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

  protected readonly LetDbChgStore _LetDbChgStore;
  protected readonly SrvrNameStore _SrvrNameStore;
  protected readonly DtBsNameStore _DtBsNameStore;
  protected readonly GSReportStore _GSReportStore;
  async void SrvrNameStore_Chngd(string val) { SrvrName = val; await RefreshReloadAsync(); }
  async void DtBsNameStore_Chngd(string val) { DtBsName = val; await RefreshReloadAsync(); }
  async void GSReportStore_Chngd(string val) { GSReport = val; await RefreshReloadAsync(); }
  async void LetDbChgStore_Chngd(bool value) { LetDbChg = value; await RefreshReloadAsync(); }

  [ObservableProperty] string? srvrName; partial void OnSrvrNameChanged(string? value) { if (value is not null && _inited) { _SrvrNameStore.Change(value); Bpr.Click(); UsrStgns.SrvrName = value; } }
  [ObservableProperty] string? dtBsName; partial void OnDtBsNameChanged(string? value) { if (value is not null && _inited) { _DtBsNameStore.Change(value); Bpr.Click(); UsrStgns.DtBsName = value; } }
  [ObservableProperty] string? gSReport; partial void OnGSReportChanged(string? value) { if (value is not null && _inited) {  _GSReportStore.Change(value); } }
  [ObservableProperty] bool letDbChg; partial void OnLetDbChgChanged(bool value) { _LetDbChgStore.Change(value); }

  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }

  public UserSettings UsrStgns { get; }
  public IConfigurationRoot Cfg { get; }
  public QstatsRlsContext Dbx { get; }
  public ILogger Lgr { get; }
  public IBpr Bpr { get; }
  public Window MainWin { get; }

  [ObservableProperty] bool isDevDbg;
  [ObservableProperty] ICollectionView? pageCvs;
  [ObservableProperty] string searchText;
  [ObservableProperty] bool includeClosed;
  [ObservableProperty] bool isBusy;
  [ObservableProperty][NotifyCanExecuteChangedFor(nameof(Save2DbCommand))] bool hasChanges;

  partial void OnSearchTextChanged(string value) { Bpr.Tick(); PageCvs?.Refresh(); }  //tu: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
  partial void OnIncludeClosedChanged(bool value) { Bpr.Tick(); PageCvs?.Refresh(); } //tu: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
  partial void OnIsBusyChanged(bool value) { _mainVM.IsBusy = value; ; }     /*BusyBlur = value ? 8 : 0;*/    //Write($"TrcW:>         ├── BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n");

  [RelayCommand] protected void ChkDb4Cngs() { Bpr.Click(); GSReport = Dbx.GetDbChangesReport() + $"{(LetDbChg ? "" : "\n RO - user!!!")}"; HasChanges = Dbx.HasUnsavedChanges(); WriteLine(GSReport); }
  [RelayCommand] protected async Task Save2Db() { try { Bpr.Click(); IsBusy = _saving = true; _ = await SaveLogReportOrThrow(Dbx); } catch (Exception ex) { IsBusy = false; ex.Pop(Lgr); } finally { IsBusy = _saving = false; Bpr.Tick(); } }
}