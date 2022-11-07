﻿namespace MinNavTpl.VM.VMs;
public partial class Page01VM : BaseDbVM
{
  int _thisCampaign;
  public Page01VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, EmailOfIStore eml, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110)
  {
    EmaiStore = eml; //EmaiStore.Changed += EmaiStore_Chngd;
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
      await Dbx.
        Emails.
        //Include(r => r.Ehists).
        LoadAsync();
#else //^^ VS vv    //todo: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/read-related-data?view=aspnetcore-6.0
      await Dbx.Emails.LoadAsync();
      //await Dbx.Ehists.LoadAsync();
#endif

      EmailCvs = CollectionViewSource.GetDefaultView(Dbx.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === EmailCvs = CollectionViewSource.GetDefaultView(await Dbx.Emails.ToListAsync());
      EmailCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
      EmailCvs.Filter = obj => obj is not Email lead || lead is null || string.IsNullOrEmpty(SearchText) ||
        lead.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        lead.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

      Lgr.Log(LogLevel.Trace, Report = $" ({Dbx.Emails.Local.Count:N0} + {Dbx.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  public EmailOfIStore EmaiStore { get; }

  [ObservableProperty] ICollectionView? ehistCvs;
  [ObservableProperty] ICollectionView? emailCvs;
  [ObservableProperty] Email? currentEmail;
  string _f = ""; public string SearchText { get => _f; set { if (SetProperty(ref _f, value)) EmailCvs?.Refresh(); } }
  bool? _ic; public bool? IncludeClosed { get => _ic; set { if (SetProperty(ref _ic, value)) EmailCvs?.Refresh(); } }
  Email? _e; public Email? SelectdEmail { get => _e; set { if (SetProperty(ref _e, value, true) && value is not null && _loaded) { Bpr.Click(); UsrStgns.EmailOfI = value.Id; SrvrStore.Change(value.Id); } } }

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
  [RelayCommand] void CloseEmail() {; ; }
}
