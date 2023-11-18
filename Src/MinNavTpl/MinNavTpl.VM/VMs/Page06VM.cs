using Db.MinFinInv.PowerTools.Models;

namespace MinNavTpl.VM.VMs;
public partial class Page06VM : BaseDbVM
{
    public Page06VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, MinFinInvDbContext dbi, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, 8110)
    {
        Dbi = dbi;
    }
    public override async Task<bool> InitAsync()
    {
        try
        {
            IsBusy = true;
            await Task.Delay(22); // <== does not show up without this...............................
            var sw = Stopwatch.StartNew();

            await Dbi.AcntHists.LoadAsync();
            await Dbi.InvAccounts.OrderBy(r => r.AcntHists.Count).ThenBy(r => r.RowAddedAt).ThenBy(r => r.Id).LoadAsync();

            PageCvs = CollectionViewSource.GetDefaultView(Dbi.InvAccounts.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbi.InvAccounts.ToListAsync());

            PageCvs.SortDescriptions.Add(new SortDescription(nameof(InvAccount.RowAddedAt), ListSortDirection.Descending));

            PageCvs.Filter = obj => obj is not InvAccount row || row is null || (
              string.IsNullOrEmpty(SearchText) ||
              row.AcntNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              row.DfsPlanNum?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              row.DfsClientId?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              row.Id?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);

            Lgr.Log(LogLevel.Trace, GSReport = $" {Dbi.InvAccounts.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

            return true;
        } catch (Exception ex) { ex.Pop(Lgr); return false; } finally { _ = await base.InitAsync(); }
    }
    public override Task<bool> WrapAsync() => base.WrapAsync();

    [ObservableProperty] InvAccount? selectdInvAccount;
    [ObservableProperty] InvAccount? currentInvAccount;

    public MinFinInvDbContext Dbi
    {
        get;
    }

    [RelayCommand]
    async void Scan4newCo()
    {
        IsBusy = true;
        await Task.Delay(222); // <== does not show busy anime up without this...............................
        try { } catch (Exception ex) { ex.Pop(); } finally { IsBusy = false; }
    }

    [RelayCommand]
    void CloseInvAccount()
    {
        if (SelectdInvAccount is not null) { SelectdInvAccount.Notes += " Blacklisted"; /*SelectdInvAccount.ModifiedAt = DateTime.Now;*/ }
    }
}
