namespace MinNavTpl.VM.VMs;
public partial class EmailDetailVM : BaseDbVM
{
  public EmailDetailVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110)
  {
    EmailOfIStore = eml; EmailOfIStore.Changed += EmailOfIStore_Chngd;
    EmailOfI = eml.LastVal;
  }
  public override async Task<bool> InitAsync() { IsBusy = true; _ = await InitAsyncTask(EmailOfI); return await base.InitAsync(); }
  async Task<bool> InitAsyncTask(string emailOfI, [CallerMemberName] string? cmn = "")
  {
    WriteLine($"■■ Init  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22} ■■■■");
    try
    {
      var sw = Stopwatch.StartNew();
      EmailOfI = emailOfI;

      await Task.Yield();

#if !true
      await Dbx.
        Emails.Where(r => r.Id == EmailOfI).
        Include(r => r.Ehists).
        LoadAsync();
#else //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0

      //This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.   NotSupportedException at C:\g\MiniScaffold\Src\MinNavTpl\MinNavTpl.VM\VMs\EmailDetailVM.cs(40): InitAsyncTask() 
      //System.NotSupportedException: This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.
      //await Dbx.Emails.Where(r => r.Id == EmailOfI).LoadAsync();
      //await Dbx.Ehists.Where(r => r.EmailId == EmailOfI).LoadAsync();
      Dbx.Emails.Where(r => r.Id == EmailOfI).Load();
      Dbx.Ehists.Where(r => r.EmailId == EmailOfI).Load();
#endif

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Ehists.Local.ToObservableCollection().Where(r => r.EmailId == EmailOfI)); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Ehists.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Ehist.AddedAt), ListSortDirection.Descending));
      //PageCvs.Filter = obj => obj is not Ehist lead || lead is null || string.IsNullOrEmpty(SearchText) ||        lead.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||        lead.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      GSReport = $" {Dbx.Emails.Local.Count:N0} + {Dbx.Ehists.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s";
      //Lgr.Log(LogLevel.Trace, GSReport );

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
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
      _ = await InitAsyncTask(emailOfI);
  }
  public EmailOfIStore EmailOfIStore { get; }

  [ObservableProperty] string emailOfI = "";
  [ObservableProperty] Ehist? currentEhist;
  [ObservableProperty] Ehist? selectdEhist;
}