namespace MinNavTpl.VM.VMs;
public partial class Page05VM : BaseDbVM
{
    public Page05VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, ISpeechSynth synth) : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, synth, 8110) { }
    public async override Task<bool> InitAsync()
    {
        try
        {
            IsBusy = true;
            await Task.Delay(22); // <== does not show up without this...............................
            var sw = Stopwatch.StartNew();

            await Dbq.PhoneAgencyXrefs.LoadAsync();
            await Dbq.Phones.LoadAsync();
            await Dbq.Emails.LoadAsync();
            await Dbq.Agencies.LoadAsync();

            await Dbq.Agencies.OrderByDescending(r => r.Emails.Count).ThenBy(r => r.PhoneAgencyXrefs.Count).ThenBy(r => r.Id).LoadAsync();
            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Agencies.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.Agencies.ToListAsync());
                                                                                                        //PageCvs.SortDescriptions.Add(new SortDescription(nameof(Agency.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Agency row || row is null ||
              string.IsNullOrEmpty(SearchText) ||
                row.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                  row.Id?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true /*&& (row.IsBroadcastee || IncludeClosed) */;

            //tmi: Lgr.Log(LogLevel.Trace, GSReport += $"loaded   {Dbq.Agencies.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1}  agencies/sec\n");

            return true;
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { _ = await base.InitAsync(); }
    }
    public override Task<bool> WrapAsync() => base.WrapAsync();

    [ObservableProperty] Agency? selectdAgency;
    [ObservableProperty] Agency? currentAgency;

    [RelayCommand]
    async Task Scan4newCoAsync()
    {
        IsBusy = true; await Task.Delay(222); // <== does not show busy anime up without this...............................
        var sw = Stopwatch.StartNew();

        try
        {
            foreach (var agent in Dbq.Emails.Local)
            {
                if (agent.Company is not null && !agent.Company.Equals(CsvImporterService.GetCompany(agent.Id), StringComparison.OrdinalIgnoreCase) && !agent.Id.Contains("unknwn"))
                    agent.Company = CsvImporterService.GetCompany(agent.Id);
            }

            var companyCount = Dbq.Emails.Local.GroupBy(e => e.Company?.ToLower()).Select(r => new { Company = r.Key?.ToLower(), Count = r.Count() }).ToList();

            var gSReport = "";
            var res = Parallel.ForEach(companyCount, co => //dbg: foreach(var co in companyCount)
            {
                var agency = Dbq.Agencies.Local.FirstOrDefault(r => r.Id.Equals(co.Company, StringComparison.OrdinalIgnoreCase));
                if (agency is not null)
                {
                    lock (gSReport)
                    {
                        if (agency.TtlAgents != co.Count)
                        {
                            gSReport += $"{agency.Id,-26} {agency.TtlAgents,4} => {co.Count}\n";
                            agency.TtlAgents = co.Count;
                            agency.ModifiedAt = Now;
                        }
                    }
                }
                else
                {
                    var nl = new Agency { Id = co.Company ?? "■■ No way ■■", TtlAgents = co.Count, AddedAt = Now };
                    lock (Dbq.Agencies.Local)
                    {
                        Dbq.Agencies.Local.Add(nl);
                    }
                }
            });

            GSReport += gSReport;
            GSReport += $"Parallel.ForEach took  {sw.Elapsed:mm\\:ss}\n";
            ChkDb4Cngs();
            await Bpr.TickAsync();
        }
        catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    void CloseAgency()
    {
        if (SelectdAgency is not null) { SelectdAgency.Note += " Blacklisted"; SelectdAgency.ModifiedAt = DateTime.Now; }
    }
}
