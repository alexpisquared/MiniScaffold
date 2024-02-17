namespace MinNavTpl.VM.VMs;
public partial class Page04VM : BaseDbVM
{
    public Page04VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, ISpeechSynth synth) : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, synth, 8110) { }
    public async override Task<bool> InitAsync()
    {
        try
        {
            IsBusy = true;
            await Task.Delay(22); // <== does not show up without this...............................
            var sw = Stopwatch.StartNew();

#if true
            await Dbq.Emails.Include(r => r.Leads.Where(r => r.CampaignId == _thisCampaignId)).
              //adds 28 sec!!! ThenInclude(r => r.LeadEmails). // for the case of multi agents per role
              LoadAsync();
#else      //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0
     await Dbq.Emails.LoadAsync();
     await Dbq.Leads.Where(r => r.CampaignId == _thisCampaign).LoadAsync();
#endif

            await Dbq.LkuLeadStatuses.LoadAsync();

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Leads.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.Leads.ToListAsync());

            PageCvs.SortDescriptions.Add(new SortDescription(nameof(Lead.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Lead r || r is null || string.IsNullOrEmpty(SearchText) ||
              r.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              r.OppCompany?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Leads.Local.ToObservableCollection()); // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.corestrings.databindingwithilistsource?view=efcore-6.0

            LeadStatusCvs = CollectionViewSource.GetDefaultView(Dbq.LkuLeadStatuses.Local.ToObservableCollection()); // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.corestrings.databindingwithilistsource?view=efcore-6.0

            AllEmailsList = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.corestrings.databindingwithilistsource?view=efcore-6.0

            //tmi: Lgr.Log(LogLevel.Trace, GSReport += $"loaded   {Dbq.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1}  emails/sec\n");

            return true;
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { _ = await base.InitAsync(); }
    }
    public override Task<bool> WrapAsync() => base.WrapAsync();

    [ObservableProperty] ICollectionView? allEmailsList;
    [ObservableProperty] ICollectionView? leadStatusCvs;
    [ObservableProperty] Lead? selectdLead;
    [ObservableProperty] Lead? currentLead;

    [RelayCommand]
    void AddNewLead()
    {
        try
        {
            var nl = new Lead { AddedAt = DateTime.Now, CampaignId = _thisCampaignId, Note = string.IsNullOrEmpty(Clipboard.GetText()) ? "New Lead" : Clipboard.GetText() };
            Dbq.Leads.Local.Add(nl);

            SelectdLead = nl;
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [RelayCommand]
    void CloseLead()
    {
        if (SelectdLead is not null) SelectdLead.Status = "Closed";
    }
}
