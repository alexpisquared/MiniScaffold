namespace MinNavTpl.VM.VMs;
public partial class Page04VM : BaseDbVM
{
  int _thisCampaign;

  public Page04VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Task.Delay(22); // <== does not show up without this...............................

      _thisCampaign = Dbx.Campaigns.Max(r => r.Id);

#if true
      await Dbx.
        Emails.
        Include(r => r.Leads.Where(r => r.CampaignId == _thisCampaign)).
        //adds 28 sec!!! ThenInclude(r => r.LeadEmails). // for the case of mlulti agents per role
        LoadAsync();
#else      //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0
     await Dbx.Emails.LoadAsync();
     await Dbx.Leads.Where(r => r.CampaignId == _thisCampaign).LoadAsync();
#endif 

      await Dbx.LkuLeadStatuses.LoadAsync();

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Leads.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Leads.ToListAsync());

      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Lead.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Lead r || r is null || string.IsNullOrEmpty(SearchText) ||
        r.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        r.OppCompany?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Leads.Local.ToObservableCollection()); // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.corestrings.databindingwithilistsource?view=efcore-6.0

      LeadStatusCvs = CollectionViewSource.GetDefaultView(Dbx.LkuLeadStatuses.Local.ToObservableCollection()); // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.corestrings.databindingwithilistsource?view=efcore-6.0

      Lgr.Log(LogLevel.Trace, GSReport = $" ({Dbx.Leads.Local.Count:N0} + {Dbx.Leads.Local.Count:N0} + {Dbx.LkuLeadStatuses.Local.Count:N0}) / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();

  [ObservableProperty] ICollectionView? leadStatusCvs;
  [ObservableProperty] Lead? selectdLead;
  [ObservableProperty] Lead? currentLead;

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

  [RelayCommand] void CloseLead() { if (SelectdLead is not null) SelectdLead.Status = "Closed"; }
}
