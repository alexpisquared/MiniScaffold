namespace MinNavTpl.VM.VMs;
public partial class Page05VM : BaseDbVM
{
  public Page05VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Task.Delay(22); // <== does not show up without this...............................

      await Dbx.Agencies.LoadAsync();
      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Agencies.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Agencies.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Agency.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Agency row || row is null || string.IsNullOrEmpty(SearchText) ||
        row.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        row.Id?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      Lgr.Log(LogLevel.Trace, GSReport = $" {Dbx.Agencies.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  [ObservableProperty] Agency? selectdAgency;
  [ObservableProperty] Agency? currentAgency;

  [RelayCommand]
  async void Scan4newCo()
  {
    IsBusy = true;
    await Task.Delay(222); // <== does not show busy anime up without this...............................
    try
    {
      var emailAdrss = await Dbx.Emails.
        GroupBy(e => e.Company).
        Select(r => new { Company = r.Key, Count = r.Count() }).ToListAsync();

      emailAdrss.ForEach(eml =>
      {
        var exstg = Dbx.Agencies.Local.FirstOrDefault(r => r.Id.Equals(eml.Company, StringComparison.OrdinalIgnoreCase));
        if (exstg is not null)
        {
          if (exstg.TtlAgents != eml.Count)
          {
            exstg.TtlAgents = eml.Count;
            exstg.ModifiedAt = Now;
          }
        }
        else
        {
          var nl = new Agency { Id = eml.Company ?? "■■ No way ■■", TtlAgents = eml.Count, AddedAt = Now };
          Dbx.Agencies.Local.Add(nl);
        }
      });

      ChkDb4Cngs();      //GSReport = await SaveLogReportOrThrow(Dbx, "new agencies");
    }
    catch (Exception ex) { ex.Pop(); }
    finally { IsBusy = false; }
  }

  [RelayCommand] void CloseAgency() { if (SelectdAgency is not null) { SelectdAgency.Note += " Blacklisted"; SelectdAgency.ModifiedAt = DateTime.Now; } }
}
