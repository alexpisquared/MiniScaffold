namespace MinNavTpl.VM.VMs;
public partial class Page01VM : BaseDbVM
{
  int _thisCampaign;
  public Page01VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110)
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

      await Dbx.Emails.LoadAsync();

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Emails.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Email row || row is null || string.IsNullOrEmpty(SearchText) ||
        row.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        row.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      Lgr.Log(LogLevel.Trace, Report = $" ({Dbx.Emails.Local.Count:N0} + {Dbx.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  public EmailOfIStore EmailOfIStore { get; }
  public EmailDetailVM EmailOfIVM { get; }

  [ObservableProperty] ICollectionView? ehistCvs;
  [ObservableProperty] Email? currentEmail;
  Email? _e; public Email? SelectdEmail { get => _e; set { if (SetProperty(ref _e, value, true) && value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } }

  [RelayCommand]
  void AddNewEmail()
  {
    try
    {
      var newEml = new Email { AddedAt = DateTime.Now, Notes = string.IsNullOrEmpty(Clipboard.GetText()) ? "New Email" : Clipboard.GetText() };
      Dbx.Emails.Local.Add(newEml);

      SelectdEmail = newEml;
    }
    catch (Exception ex) { ex.Pop(); }
  }
  [RelayCommand] void CloseEmail() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Del() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Cou() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void PBR() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Nxt() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void OLk() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void DNN() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
}
