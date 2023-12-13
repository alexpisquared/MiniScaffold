using System.Windows.Threading;

namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
    readonly int _hashCode;
    readonly ISecurityForcer _secForcer;
    bool _inited;
    protected MainVM MainVM
    {
        get;
    }
    protected bool _saving, _loading;
    protected readonly DateTime Now = DateTime.Now;
    public BaseDbVM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, /*EmailOfIStore eml,*/ LetDbChgStore awd, UserSettings usrStgns, ISpeechSynth synth, int oid)
    {
        IsDevDbg = VersionHelper.IsDbg;

        searchText = "";

        Lgr = lgr;
        Cfg = cfg;
        Dbq = dbq;
        Bpr = bpr;
        MainWin = (Window)win;
        UsrStgns = usrStgns;
        MainVM = mainVM;
        Synth = synth;
        _secForcer = sec;
        _hashCode = GetType().GetHashCode();

        letDbChg = UsrStgns.LetDbChg;

        _SrvrNameStore = svr; _SrvrNameStore.Changed += SrvrNameStore_ChngdAsync;
        _DtBsNameStore = dbs; _DtBsNameStore.Changed += DtBsNameStore_ChngdAsync;
        _GSReportStore = gsr; _GSReportStore.Changed += GSReportStore_ChngdAsync;
        _LetDbChgStore = awd; _LetDbChgStore.Changed += LetDbChgStore_ChngdAsync;

        _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); } }, DispatcherPriority.Normal); //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

        Lgr.LogInformation($"┌──{GetType().Name,-16} eo-ctor      PageRank:{oid}");
    }
    public async override Task<bool> InitAsync()
    {
        IsBusy = false;
        _inited = true;
        return await base.InitAsync();
    }
    public async override Task<bool> WrapAsync()
    {
        try
        {
            if (LetDbChg && Dbq.HasUnsavedChanges())
            {
                var rv = MessageBoxResult.Yes; // MessageBox.Show("Would you like to save the changes?\r\n\n..or select Cancel to stay on the page", "There are unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (rv)
                {
                    default:
                    case MessageBoxResult.Cancel: return false;
                    case MessageBoxResult.No: Dbq.DiscardChanges(); break;
                    case MessageBoxResult.Yes:
                        var report = await SaveLogReportOrThrowAsync(Dbq);
                        GSReport += $"\n{report}";
                        break;
                }
            }

            _SrvrNameStore.Changed -= SrvrNameStore_ChngdAsync;
            _DtBsNameStore.Changed -= DtBsNameStore_ChngdAsync;
            _GSReportStore.Changed -= GSReportStore_ChngdAsync;
            _LetDbChgStore.Changed -= LetDbChgStore_ChngdAsync;

            return true;
        } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; IsBusy = false; ex.Pop(Lgr); return false; } finally
        {
            Lgr.LogInformation($"└──{GetType().Name,-16} eo-wrap     _hash:{_hashCode,-10}   br.hash:{Dbq.GetType().GetHashCode(),-10}  ");
        }
    }
    public override void Dispose()
    {
        _SrvrNameStore.Changed -= SrvrNameStore_ChngdAsync;
        _DtBsNameStore.Changed -= DtBsNameStore_ChngdAsync;
        _GSReportStore.Changed -= GSReportStore_ChngdAsync;
        _LetDbChgStore.Changed -= LetDbChgStore_ChngdAsync;

        base.Dispose();
    }
    public async virtual Task RefreshReloadAsync([CallerMemberName] string? cmn = "")
    {
        WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() ");
        await Task.Yield();
    }
    protected void ReportProgress(string msg)
    {
        GSReport = msg; Lgr.Log(LogLevel.Trace, msg);
    }

    public ISpeechSynth Synth
    {
        get;
    }

    protected async Task<string> SaveLogReportOrThrowAsync(DbContext dbq, string note = "", [CallerMemberName] string? cmn = "")
    {
        if (LetDbChg)
        {
            var (success, rowsSaved, report) = await dbq.TrySaveReportAsync($"{note} {cmn} calls ");
            if (!success)
            {
                throw new Exception(report);
            }

            Lgr.LogInformation(report);
            GSReport = report;
        }
        else
        {
            GSReport = $"Current user permission \n\n    {_secForcer.PermisssionCSV} \n\ndoes not include database modifications.";
            Lgr.LogWarning(GSReport.Replace("\n", "") + "▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ");
            await Bpr.BeepAsync(333, 2.5); // _ = MessageBox.Show(report, $"Not enough priviliges \t\t {DateTime.Now:MMM-dd HH:mm}", MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        return GSReport;
    }

    protected readonly LetDbChgStore _LetDbChgStore;
    protected readonly SrvrNameStore _SrvrNameStore;
    protected readonly DtBsNameStore _DtBsNameStore;
    protected readonly GSReportStore _GSReportStore;
    async void SrvrNameStore_ChngdAsync(string val)
    {
        try { SrvrName = val; await RefreshReloadAsync(); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; Lgr.LogError(ex, $"SrvrNameStore_ChngdAsync({val})"); }
    }
    async void DtBsNameStore_ChngdAsync(string val)
    {
        DtBsName = val; await RefreshReloadAsync();
    }
    async void GSReportStore_ChngdAsync(string val)
    {
        GSReport = val; await RefreshReloadAsync();
    }
    async void LetDbChgStore_ChngdAsync(bool value)
    {
        LetDbChg = value; await RefreshReloadAsync();
    }

    [ObservableProperty] string? srvrName; partial void OnSrvrNameChanged(string? value)
    {
        if (value is not null && _inited) { _SrvrNameStore.Change(value); Bpr.Click(); UsrStgns.SrvrName = value; }
    }
    [ObservableProperty] string? dtBsName; partial void OnDtBsNameChanged(string? value)
    {
        if (value is not null && _inited) { _DtBsNameStore.Change(value); Bpr.Click(); UsrStgns.DtBsName = value; }
    }
    [ObservableProperty] string? gSReport; partial void OnGSReportChanged(string? value)
    {
        if (value is not null /*&& _inited*/) { _GSReportStore.Change(value); }
    }
    [ObservableProperty] bool letDbChg; partial void OnLetDbChgChanged(bool value)
    {
        _LetDbChgStore.Change(value);
    }

    ADUser? _ct; public ADUser? CurentUser
    {
        get => _ct; set
        {
            if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); }
        }
    }

    public UserSettings UsrStgns
    {
        get;
    }
    public IConfigurationRoot Cfg
    {
        get;
    }
    public QstatsRlsContext Dbq
    {
        get;
    }
    public ILogger Lgr
    {
        get;
    }
    public IBpr Bpr
    {
        get;
    }
    public Window MainWin
    {
        get;
    }

    [ObservableProperty] bool isDevDbg;
    [ObservableProperty] ICollectionView? pageCvs;
    [ObservableProperty] string searchText;
    [ObservableProperty] bool includeClosed;
    [ObservableProperty] bool isBusy;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(Save2DbCommand))] bool hasChanges;

    partial void OnSearchTextChanged(string value)
    {
        Bpr.Tick(); PageCvs?.Refresh();
    }  //tu: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnIncludeClosedChanged(bool value)
    {
        var emailQuery = DevOps.IsDbg ? Dbq.Emails.Where(r => IncludeClosed || r.Id.Contains("reply.l") || r.Id.Contains("reply.f") || r.Id.Contains("reply.f")).OrderBy(r => r.LastAction) :
            Dbq.Emails.Where(r => IncludeClosed || Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id) == true).OrderBy(r => r.NotifyPriority);
        emailQuery.Load(); //tmi: Lgr.Log(LogLevel.Trace, emailQuery.ToQueryString());

        PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());

        PageCvs?.Refresh();
    }
    partial void OnIsBusyChanged(bool value) => MainVM.IsBusy = value; /*BusyBlur = value ? 8 : 0;*/    //Write($"TrcW:>         ├──BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n");

    [RelayCommand]
    protected void ChkDb4Cngs()
    {
        Bpr.Click(); GSReport = Dbq.GetDbChangesReport() + $"{(LetDbChg ? "" : "\n RO - user!!!")}"; HasChanges = Dbq.HasUnsavedChanges(); WriteLine(GSReport);
    }

    [RelayCommand]
    protected async Task Save2Db()
    {
        try { Bpr.Click(); IsBusy = _saving = true; _ = await SaveLogReportOrThrowAsync(Dbq); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; IsBusy = false; ex.Pop(Lgr); } finally { IsBusy = _saving = false; Bpr.Tick(); }
    }
}