namespace MinNavTpl.VM.VMs;
public partial class Page05VM : BaseDbVM
{
  int _thisCampaign;

  public Page05VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Task.Delay(22); // <== does not show up without this...............................

      _thisCampaign = Dbx.Campaigns.Max(r => r.Id);

      await Dbx.Agencies.LoadAsync();

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Agencies.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Agencies.ToListAsync());

      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Agency.Id), ListSortDirection.Ascending));
      PageCvs.Filter = obj => obj is not Agency Agency || Agency is null || string.IsNullOrEmpty(SearchText) ||
        Agency.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        Agency.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Agencies.Local.ToObservableCollection());


      Lgr.Log(LogLevel.Trace, Report = $" {Dbx.Agencies.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  [ObservableProperty] ICollectionView? pageCvs;
  [ObservableProperty] Agency? selectdAgency;
  [ObservableProperty] Agency? currentAgency;

  [RelayCommand]
  async void Scan4newCo()
  {
    IsBusy = true;
    try
    {
      var emailAdrss = await Dbx.Emails.Select(r => r.Id).ToListAsync();
      emailAdrss.ForEach(eml =>
      {
        var agency = CsvImporterService.GetCompany(eml);
        if (Dbx.Agencies.Local.Any(r => r.Id.Equals(agency)))
          return;

        var nl = new Agency { Id = agency, AddedAt = DateTime.Now };
        Dbx.Agencies.Local.Add(nl);
      });

      ChkDb4Cngs();      //Report = await SaveLogReportOrThrow(Dbx, "new agencies");
    }
    catch (Exception ex) { ex.Pop(); }
    finally { IsBusy = false; }
  }

  [RelayCommand] void CloseAgency() { if (SelectdAgency is not null) { SelectdAgency.Note += " Blacklisted"; SelectdAgency.ModifiedAt = DateTime.Now; } }
}
