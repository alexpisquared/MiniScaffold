namespace MinNavTpl.VM.VMs;
public partial class Page04VM : BaseDbVM
{
  int _thisCampaign;

  public Page04VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Task.Delay(22); // <== does not show up without this...............................

      _thisCampaign = Dbx.Campaigns.Max(r => r.Id);

      await Dbx.Leads.Where(r => r.CampaignId == _thisCampaign).LoadAsync();
      await Dbx.Emails.LoadAsync();
      await Dbx.LkuLeadStatuses.LoadAsync();
      
      LeadCvs = CollectionViewSource.GetDefaultView(Dbx.Leads.Local.ToObservableCollection());
      LeadCvs.SortDescriptions.Add(new SortDescription(nameof(Lead.AddedAt), ListSortDirection.Ascending));
      LeadCvs.Filter = obj => obj is not Lead lead || lead is null || string.IsNullOrEmpty(SearchText) ||
        lead.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        lead.OppCompany?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      EmailCvs = CollectionViewSource.GetDefaultView(Dbx.Emails.Local.ToObservableCollection());

      LeadStatusCvs = CollectionViewSource.GetDefaultView(Dbx.LkuLeadStatuses.Local.ToObservableCollection());

      Lgr.Log(LogLevel.Trace, Report = $" {Dbx.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  [ObservableProperty] ICollectionView? leadCvs;
  [ObservableProperty] ICollectionView? emailCvs;
  [ObservableProperty] ICollectionView? leadStatusCvs;
  [ObservableProperty] Lead? selectdLead;
  [ObservableProperty] Lead? currentLead;
  string _f = ""; public string SearchText { get => _f; set { if (SetProperty(ref _f, value)) LeadCvs?.Refresh(); } }
  bool? _ic; public bool? IncludeClosed { get => _ic; set { if (SetProperty(ref _ic, value)) LeadCvs?.Refresh(); } }

  [RelayCommand]
  void AddNewLead()
  {
    try
    {
      var nl = new Lead { AddedAt = DateTime.Now, CampaignId = _thisCampaign, Note = string.IsNullOrEmpty(Clipboard.GetText()) ? "New Lead" : Clipboard.GetText() };
      Dbx.Leads.Local.Add(nl);

      SelectdLead = nl;
    }
    catch (Exception ex) { ex.Pop(); }
  }
  [RelayCommand] void ChkDb4Cngs() {; ; }
  [RelayCommand] void CloseLead() {; ; }
}
