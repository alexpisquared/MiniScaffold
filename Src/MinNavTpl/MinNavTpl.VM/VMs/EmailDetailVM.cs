using StandardLib.Extensions;

namespace MinNavTpl.VM.VMs;
public partial class EmailDetailVM : BaseDbVM
{
    public EmailDetailVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, ISpeechSynth synth) : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, synth, 8110)
    {
        EmailOfIStore = eml; EmailOfIStore.Changed += EmailOfIStore_Chngd;
        EmailOfI = eml.LastVal;
    }
    public async override Task<bool> InitAsync()
    {
        IsBusy = true; _ = await InitTaskAsync(EmailOfI); return await base.InitAsync();
    }
    async Task<bool> InitTaskAsync(string emailOfI, [CallerMemberName] string? cmn = "")
    {
        WriteLine($"■■ Init  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22} ■■■■");
        try
        {
            var sw = Stopwatch.StartNew();
            EmailOfI = emailOfI;

            await Task.Yield();

#if !true
      await Dbq.
        Emails.Where(r => r.Id == EmailOfI).
        Include(r => r.Ehists).
        LoadAsync();
#else //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0

            //This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.   NotSupportedException at C:\g\MiniScaffold\Src\MinNavTpl\MinNavTpl.VM\VMs\EmailDetailVM.cs(40): InitTaskAsync() 
            //System.NotSupportedException: This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.
            //await Dbq.Emails.Where(r => r.Id == EmailOfI).LoadAsync();
            //await Dbq.Ehists.Where(r => r.EmailId == EmailOfI).LoadAsync();
            Dbq.Emails.Where(r => r.Id == EmailOfI).Load();
            if (Dbq.Ehists.Any(r => r.EmailId == EmailOfI))
                try { Dbq.Ehists.Where(r => r.EmailId == EmailOfI).Load(); }
                catch (Exception ex)
                {
                    var sql = Dbq.Ehists.Where(r => r.EmailId == EmailOfI).ToQueryString(); // SELECT[e].[ID], [e].[AddedAt], [e].[EMailID], [e].[EmailedAt], [e].[LetterBody], [e].[LetterSubject], [e].[Notes], [e].[RecivedOrSent], [e].[SentOn] FROM[EHist] AS[e] WHERE[e].[EMailID] = 'jill.mellish@robertson.ca'
                    ex.Log($"{emailOfI} //todo: Why LetterSubject=null causes the issue?\n  {sql}");
                }
#endif

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Ehists.Local.ToObservableCollection().Where(r => r.EmailId == EmailOfI)); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.Ehists.ToListAsync());
            PageCvs.SortDescriptions.Add(new SortDescription(nameof(Ehist.AddedAt), ListSortDirection.Descending));
            //PageCvs.Filter = obj => obj is not Ehist lead || lead is null || string.IsNullOrEmpty(SearchText) ||        lead.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||        lead.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            GSReport += $" {Dbq.Emails.Local.Count:N0} + {Dbq.Ehists.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s";
            //Lgr.Log(LogLevel.Trace, GSReport );

            return true;
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { IsBusy = false; }
    }
    public override void Dispose()
    {
        EmailOfIStore.Changed -= EmailOfIStore_Chngd;
        base.Dispose();
    }

    async void EmailOfIStore_Chngd(string emailOfI, [CallerMemberName] string? cmn = "")
    {
        WriteLine($"■■ EmDt  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22}  {(EmailOfI != emailOfI ? "==>Load" : "==>----")}");
        if (EmailOfI != emailOfI)
        {
            _ = await InitTaskAsync(emailOfI);
        }
    }
    public EmailOfIStore EmailOfIStore
    {
        get;
    }

    [ObservableProperty] string emailOfI = "";
    [ObservableProperty] Ehist? currentEhist;
    [ObservableProperty] Ehist? selectdEhist;
}