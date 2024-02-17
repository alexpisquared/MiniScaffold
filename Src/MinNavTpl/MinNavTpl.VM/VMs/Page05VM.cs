﻿namespace MinNavTpl.VM.VMs;
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
            PageCvs.Filter = obj => obj is not Agency row || row is null || (
              string.IsNullOrEmpty(SearchText) ||
              row.Note?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              row.Id?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
              //&& (row.IsBroadcastee || IncludeClosed)
              ;

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
    async void Scan4newCo()
    {
        IsBusy = true; await Task.Delay(222); // <== does not show busy anime up without this...............................

        try
        {
            foreach (var item in Dbq.Emails.Local)
            {
                if (item.Company is not null && item.Company != CsvImporterService.GetCompany(item.Company))
                    item.Company = CsvImporterService.GetCompany(item.Company);
            }

            var companyCount = Dbq.Emails.Local.
              GroupBy(e => e.Company).
              Select(r => new { Company = r.Key, Count = r.Count() }).ToList();

            Parallel.ForEach(companyCount, eml =>
            {
                var agency = Dbq.Agencies.Local.FirstOrDefault(r => r.Id.Equals(eml.Company, StringComparison.OrdinalIgnoreCase));
                if (agency is not null)
                {
                    if (agency.TtlAgents != eml.Count)
                    {
                        agency.TtlAgents = eml.Count;
                        agency.ModifiedAt = Now;
                    }
                }
                else
                {
                    var nl = new Agency { Id = eml.Company ?? "■■ No way ■■", TtlAgents = eml.Count, AddedAt = Now };
                    Dbq.Agencies.Local.Add(nl);
                }
            });

            Bpr.Tick();
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
