namespace MinNavTpl.VM.VMs;
public partial class EmailDetailVM : BaseDbVM
{
  public EmailDetailVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, EmailOfIStore eml, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110)
  {
    EmaiStore = eml; EmaiStore.Changed += EmaiStore_Chngd;
    EmailOfIProp = eml.LastVal;
  }
  public override async Task<bool> InitAsync() { _ = await InitAsyncTask(EmailOfIProp); return await base.InitAsync(); }
  async Task<bool> InitAsyncTask(string emailOfI, [CallerMemberName] string? cmn = "")
  {
    WriteLine($"■■ Init  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22} ■■■■");
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();
      EmailOfIProp = emailOfI;

      await Task.Delay(22); // <== does not show up without this...............................

#if !true
      await Dbx.
        Emails.Where(r => r.Id == EmailOfIProp).
        Include(r => r.Ehists).
        LoadAsync();
#else //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0

      //This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.   NotSupportedException at C:\g\MiniScaffold\Src\MinNavTpl\MinNavTpl.VM\VMs\EmailDetailVM.cs(40): InitAsyncTask() 
      //System.NotSupportedException: This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.
      //await Dbx.Emails.Where(r => r.Id == EmailOfIProp).LoadAsync();
      //await Dbx.Ehists.Where(r => r.EmailId == EmailOfIProp).LoadAsync();
      Dbx.Emails.Where(r => r.Id == EmailOfIProp).Load();
      Dbx.Ehists.Where(r => r.EmailId == EmailOfIProp).Load();
#endif

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Ehists.Local.ToObservableCollection().Where(r => r.EmailId == EmailOfIProp)); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Ehists.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Ehist.AddedAt), ListSortDirection.Descending));
      //PageCvs.Filter = obj => obj is not Ehist lead || lead is null || string.IsNullOrEmpty(SearchText) ||        lead.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||        lead.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      Report = $" {Dbx.Emails.Local.Count:N0} + {Dbx.Ehists.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s";
      //Lgr.Log(LogLevel.Trace, Report );

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { IsBusy = false; }
  }
  public override void Dispose()
  {
    EmaiStore.Changed -= EmaiStore_Chngd;
    base.Dispose();
  }

  async void EmaiStore_Chngd(string emailOfI, [CallerMemberName] string? cmn = "")
  {
    WriteLine($"■■ EmDt  {GetCaller(),20}  called by  {cmn,-22} {emailOfI,-22}  {(EmailOfIProp != emailOfI ? "==>Load" : "==>----")}");
    if (EmailOfIProp != emailOfI)
      _ = await InitAsyncTask(emailOfI);
    //else
  }
  public EmailOfIStore EmaiStore { get; }
  string _em = "°"; public string EmailOfIProp { get => _em; set => SetProperty(ref _em, value); }

  [ObservableProperty] Ehist? currentEhist;
  [ObservableProperty] Ehist? selectdEhist;
}