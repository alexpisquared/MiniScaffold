namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm)
      : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, eml, evm, 8110) { }
    public async override Task<bool> InitAsync()
    {
        var rv = false;
        try
        {
            IsBusy = true;
            Bpr.Start(8);

            var sw = Stopwatch.StartNew();

            await Dbq.PhoneEmailXrefs.LoadAsync();
            await Dbq.Phones.LoadAsync();
            await Dbq.Emails
                .Where(r => Dbq.VEmailAvailProds.Select(r => r.Id).Contains(r.Id))
                .OrderBy(r => r.NotifyPriority)
                .LoadAsync();

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());
            //redundant: PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Email r || r is null || string.IsNullOrEmpty(SearchText) ||
              r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            Lgr.Log(LogLevel.Trace, GSReport = $" ({Dbq.VEmailAvailProds.Local.Count:N0} + {Dbq.VEmailAvailProds.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

            Bpr.Finish(8);
        }
        catch (Exception ex) { ex.Pop(Lgr); return false; }
        finally { rv = await base.InitAsync(); }

        return rv;
    }

    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.
    [ObservableProperty] Email? selectdEmail; partial void OnSelectdEmailChanged(Email? value)
    {
        if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); }
    } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
}