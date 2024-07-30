using MinNavTpl.Stores;

namespace MinNavTpl.VM.VMs;
public partial class BaseDbVM : BaseMinVM
{
    readonly ISecurityForcer _secForcer;
    bool _inited;

    protected bool _saving, _loading;
    protected readonly DateTime Now = DateTime.Now;
    protected int _thisCampaignId;
    public BaseDbVM(ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, /*EmailOfIStore eml,*/ LetDbChgStore awd, IsBusy__Store bzi, UserSettings usrStgns, ISpeechSynth synth, int oid)
    {
        IsDevDbg = VersionHelper.IsDbg;

        searchText = "";

        Lgr = lgr;
        Cfg = cfg;
        Dbq = dbq;
        Bpr = bpr;
        MainWin = (Window)win;
        UsrStgns = usrStgns;
        Synth = synth;
        _secForcer = sec;

        letDbChg = UsrStgns.LetDbChg;

        _SrvrNameStore = svr; _SrvrNameStore.Changed += SrvrNameStore_ChngdAsyncVoid;
        _DtBsNameStore = dbs; _DtBsNameStore.Changed += DtBsNameStore_ChngdAsyncVoid;
        _GSReportStore = gsr; _GSReportStore.Changed += GSReportStore_ChngdAsyncVoid;
        _LetDbChgStore = awd; _LetDbChgStore.Changed += LetDbChgStore_ChngdAsyncVoid;
        _IsBusy__Store = bzi; _IsBusy__Store.Changed += IsBusy__Store_ChngdAsyncVoid;

        _thisCampaignId = Dbq.Campaigns.Max(r => r.Id);

        _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); } }, DispatcherPriority.Normal); //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

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
                    case MessageBoxResult.Yes: await SaveLogReportOrThrowAsync(Dbq); break;
                }
            }

            _SrvrNameStore.Changed -= SrvrNameStore_ChngdAsyncVoid;
            _DtBsNameStore.Changed -= DtBsNameStore_ChngdAsyncVoid;
            _GSReportStore.Changed -= GSReportStore_ChngdAsyncVoid;
            _LetDbChgStore.Changed -= LetDbChgStore_ChngdAsyncVoid;
            _IsBusy__Store.Changed -= IsBusy__Store_ChngdAsyncVoid;

            return true;
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; IsBusy = false; ex.Pop(Lgr); return false; }
        finally
        {
            Lgr.LogInformation($"└──{GetType().Name,-16} eo-wrap");
        }
    }
    public override void Dispose()
    {
        _SrvrNameStore.Changed -= SrvrNameStore_ChngdAsyncVoid;
        _DtBsNameStore.Changed -= DtBsNameStore_ChngdAsyncVoid;
        _GSReportStore.Changed -= GSReportStore_ChngdAsyncVoid;
        _LetDbChgStore.Changed -= LetDbChgStore_ChngdAsyncVoid;
        _IsBusy__Store.Changed -= IsBusy__Store_ChngdAsyncVoid;

        base.Dispose();
    }
    public async virtual Task RefreshReloadAsync([CallerMemberName] string? cmn = "")
    {
        WriteLine($"TrWL:> {cmn}->BaseDbVM.RefreshReloadAsync() ");
        await Task.Yield();
    }
    protected void ReportProgress(string msg)
    {
        GSReport += msg; Lgr.Log(LogLevel.Trace, msg);
    }

    public ISpeechSynth Synth
    {
        get;
    }

    protected async Task<string> SaveLogReportOrThrowAsync(DbContext dbq, string note = "", [CallerMemberName] string? cmn = "")
    {
        if (LetDbChg)
        {
            var (success, _, report) = await dbq.TrySaveReportAsync($"{note} {cmn} ->");
            if (!success)
            {
                throw new Exception(report);
            }

            Lgr.LogInformation(report);
            GSReport += $"{report}\n";
        }
        else
        {
            GSReport += $"Current user permission \n\n    {_secForcer.PermisssionCSV} \n\ndoes not include database modifications.";
            Lgr.LogWarning(GSReport.Replace("\n", "") + "▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ▓▓  ");
            await Bpr.BeepAsync(333, 2.5); // _ = MessageBox.Show(report, $"Not enough priviliges \t\t {DateTime.Now:MMM-dd HH:mm}", MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        return GSReport;
    }

    protected readonly LetDbChgStore _LetDbChgStore;
    protected readonly IsBusy__Store _IsBusy__Store;
    protected readonly SrvrNameStore _SrvrNameStore;
    protected readonly DtBsNameStore _DtBsNameStore;
    protected readonly GSReportStore _GSReportStore;
    async void SrvrNameStore_ChngdAsyncVoid(string val)
    {
        try { if (SrvrName != val) SrvrName = val; await RefreshReloadAsync(); } catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; Lgr.LogError(ex, $"SrvrNameStore_ChngdAsyncVoid({val})"); }
    }
    async void DtBsNameStore_ChngdAsyncVoid(string val)
    {
        if (DtBsName != val) DtBsName = val; await RefreshReloadAsync();
    }
    async void GSReportStore_ChngdAsyncVoid(string val)
    {
        GSReport = val; await RefreshReloadAsync();
    }
    async void LetDbChgStore_ChngdAsyncVoid(bool value)
    {
        if (LetDbChg != value) LetDbChg = value; await RefreshReloadAsync();
    }
    async void IsBusy__Store_ChngdAsyncVoid(bool value)
    {
        if (IsBusy__ != value) IsBusy__ = value; await RefreshReloadAsync();
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
    [ObservableProperty] bool isBusy__; partial void OnIsBusy__Changed(bool value)
    {
        _IsBusy__Store.Change(value);
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
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(Save2DBaseCommand))] bool hasChanges;

    partial void OnSearchTextChanged(string value)
    {
        Bpr.Tick(); PageCvs?.Refresh(); //tu: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    }
    partial void OnIncludeClosedChanged(bool value)
    {
        Bpr.Tick();

        var emailQuery = DevOps.IsDbg ?
            Dbq.Emails.Where(r => IncludeClosed || r.Id.Contains("reply.l") || r.Id.Contains("reply.f") || r.Id.Contains("reply.f")).OrderBy(r => r.LastAction) :
            Dbq.Emails.Where(r => IncludeClosed || Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id) == true).OrderBy(r => r.NotifyPriority);

        emailQuery.Load(); //tmi: Lgr.Log(LogLevel.Trace, emailQuery.ToQueryString());

        PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());

        PageCvs?.Refresh();
    }
    partial void OnIsBusyChanged(bool value)
    {
        IsBusy__ = value;
        //MainVM.IsBusy = value; 
        /*BusyBlur = value ? 8 : 0;*/    //Write($"TrcW:>         ├──BaseDbVM.IsBusy set to  {value,-5}  {(value ? "<<<<<<<<<<<<" : ">>>>>>>>>>>>")}\n");
    }

    [RelayCommand]
    protected void ChkDb4Cngs()
    {
        Bpr.Click();
        GSReport += Dbq.GetDbChangesReport() + $"{(LetDbChg ? "\n" : "\n RO - user!!!\n")}";
        HasChanges = Dbq.HasUnsavedChanges();
        WriteLine(GSReport);
    }

    [RelayCommand]
    protected async Task Save2DBaseAsync()
    {
        try
        {
            await Bpr.ClickAsync();
            IsBusy = _saving = true;
            _ = await SaveLogReportOrThrowAsync(Dbq);
            await Bpr.TickAsync();
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; IsBusy = false; ex.Pop(Lgr); }
        finally
        {
            IsBusy = _saving = false;
        }
    }
}