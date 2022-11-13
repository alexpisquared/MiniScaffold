namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseDbVM
{
  int _thisCampaign;
  public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110)
  {
    EmailOfIStore = eml; //EmailOfIStore.Changed += EmailOfIStore_Chngd;
    EmailOfIVM = evm;
  }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();

      await Task.Delay(22); // <== does not show up without this...............................

      _thisCampaign = Dbx.Campaigns.Max(r => r.Id);

#if true
      await Dbx.VEmailAvailProds.LoadAsync();
#else //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0
      await Dbx.VEmailAvailProds.LoadAsync();
#endif

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.VEmailAvailProds.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.VEmailAvailProds.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Email lead || lead is null || string.IsNullOrEmpty(SearchText) ||
        lead.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        lead.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      Lgr.Log(LogLevel.Trace, GSReport = $" ({Dbx.VEmailAvailProds.Local.Count:N0} + {Dbx.VEmailAvailProds.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();

  public EmailOfIStore EmailOfIStore { get; }
  public EmailDetailVM EmailOfIVM { get; }

  [ObservableProperty] ICollectionView? ehistCvs;
  [ObservableProperty] Email? currentEmail;
  Email? _e; public Email? SelectdEmail { get => _e; set { if (SetProperty(ref _e, value, true) && value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } }

  [RelayCommand] void AddNewEmail() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void CloseEmail() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Del() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Cou() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void PBR() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Nxt() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void OLk() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
}
