namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm)
      : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, eml, evm, 8110) { }
    public async override Task<bool> InitAsync()
    {
        try
        {
            IsBusy = true;
            Bpr.Start(8);

            var sw = Stopwatch.StartNew();
            var rv = await base.InitAsync();

            await Dbx.VEmailAvailProds.LoadAsync();
            PageCvs = CollectionViewSource.GetDefaultView(Dbx.VEmailAvailProds.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.VEmailAvailProds.ToListAsync());
            PageCvs.SortDescriptions.Add(new SortDescription(nameof(VEmailAvailProd.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not VEmailAvailProd r || r is null || string.IsNullOrEmpty(SearchText) ||
              r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            Lgr.Log(LogLevel.Trace, GSReport = $" ({Dbx.VEmailAvailProds.Local.Count:N0} + {Dbx.VEmailAvailProds.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

            Bpr.Finish(8);
            return rv;
        }
        catch (Exception ex) { ex.Pop(Lgr); return false; }
        finally { _ = await base.InitAsync(); }
    }

    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] VEmailAvailProd? currentEmail; // demo only.
    [ObservableProperty] VEmailAvailProd? selectdEmail; partial void OnSelectdEmailChanged(VEmailAvailProd? value) { if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
}