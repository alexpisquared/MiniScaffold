namespace MinNavTpl.VM.VMs;
public partial class Page04VM : BaseDbVM
{
  public Page04VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, AllowWriteDBStore allowWriteDBStore, UserSettings usrStgns) : base(mainVM, lgr, cfg, bpr, sec, dbx, win, allowWriteDBStore, usrStgns, 8440) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Dbx.Leads.LoadAsync();
      LeadCvs = CollectionViewSource.GetDefaultView(Dbx.Leads.Local.ToObservableCollection());
      LeadCvs.SortDescriptions.Add(new SortDescription(nameof(Lead.AddedAt), ListSortDirection.Descending));
      LeadCvs.Filter = obj => obj is not Lead lead || lead is null || string.IsNullOrEmpty(SearchText) || 
        lead.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true || 
        lead.OppCompany?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;


      Lgr.Log(LogLevel.Trace, $"DB:  in {sw.ElapsedMilliseconds,8}ms  at SQL:{UserSetgs.PrefSrvrName} ▀▄▀▄▀▄▀▄▀");
      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();

  void ReportProgress(string msg) => ReportMessage = msg;

  [ObservableProperty] string reportMessage = ":0";
  [ObservableProperty] ICollectionView? leadCvs;
  string _xh = ""; public string SearchText { get => _xh; set { if (SetProperty(ref _xh, value)) LeadCvs?.Refresh(); } }

  public override void Dispose() => base.Dispose();
}
