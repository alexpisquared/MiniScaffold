namespace MinNavTpl.VM.VMs;
public partial class Page02VM : Email0VM
{
  public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm)
    : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, eml, evm, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();
      var rv = await base.InitAsync(); ;

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

      return rv;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
}
public partial class Page01VM : Email0VM
{
  public Page01VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm)
    : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, eml, evm, 8110) { }
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var sw = Stopwatch.StartNew();
      var rv = await base.InitAsync();

      await Dbx.Emails.LoadAsync();
      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Emails.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Email r || r is null ||
        ((string.IsNullOrEmpty(SearchText) ||
          r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
          r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) &&
        (IncludeClosed == true || (string.IsNullOrEmpty(r.PermBanReason) && _badEmails is not null && !_badEmails.Contains(r.Id))));

      Lgr.Log(LogLevel.Trace, GSReport = $" {PageCvs?.Cast<Email>().Count():N0} / {Dbx.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return rv;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
}
public partial class Email0VM : BaseDbVM
{
  protected List<string>? _badEmails;
  int _thisCampaign;
  public Email0VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, UserSettings stg, EmailOfIStore eml, EmailDetailVM evm, int oid) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, oid)
  {
    EmailOfIStore = eml; //EmailOfIStore.Changed += EmailOfIStore_Chngd;
    EmailOfIVM = evm;
  }

  public override async Task<bool> InitAsync()
  {
    await Task.Delay(22); // <== does not show up without this...............................

    _thisCampaign = Dbx.Campaigns.Max(r => r.Id);

    _badEmails = await MiscEfDb.GetBadEmails("Select Id from [dbo].[BadEmails]()", Dbx.Database.GetConnectionString() ?? "??");

    return await base.InitAsync();
  }

  public EmailOfIStore EmailOfIStore { get; }
  public EmailDetailVM EmailOfIVM { get; }

  [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.

  [ObservableProperty][NotifyCanExecuteChangedFor(nameof(DelCommand))] Email? selectdEmail;

  partial void OnSelectdEmailChanged(Email? value) { if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty

  [RelayCommand(CanExecute = nameof(CanDel))] void Del(Email? email) { Bpr.Click(); try { _ = Dbx.Emails.Local.Remove(SelectdEmail!); } catch (Exception ex) { ex.Pop(); } }
  bool CanDel(Email? email) => email is not null; // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand
  [RelayCommand] void AddNewEmail() { try { var newEml = new Email { AddedAt = DateTime.Now, Notes = string.IsNullOrEmpty(Clipboard.GetText()) ? "New Email" : Clipboard.GetText() }; Dbx.Emails.Local.Add(newEml); SelectdEmail = newEml; } catch (Exception ex) { ex.Pop(); } }

  [RelayCommand]
  async void Cou()
  {
    Bpr.Click();
    try
    {
      if (SelectdEmail?.Fname is not null)
      {
        WriteLine($"■ ■ ■ cfg?[\"WhereAmI\"]: '{new ConfigurationBuilder().AddUserSecrets<Application>().Build()?["WhereAmI"]}'   \t <== ConfigurationBuilder().AddUserSecrets<Application>().");

        ArgumentNullException.ThrowIfNull(Cfg, "■▄▀■▄▀■▄▀■▄▀■▄▀■");
        var (ts, _, root) = await GenderApi.CallOpenAI(Cfg, SelectdEmail.Fname);

        SelectdEmail.Country = root?.country_of_origin.FirstOrDefault()?.country_name ?? root?.errmsg ?? "?????";
      }
    }
    catch (Exception ex) { ex.Pop(); }
  }
  [RelayCommand] void PBR() { Bpr.Click(); try { if (SelectdEmail is null) return; SelectdEmail.PermBanReason = $" Not an Agent - {DateTime.Today:yyyy-MM-dd}. "; Nxt(); } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void Nxt() { Bpr.Click(); try { WriteLine (PageCvs?.MoveCurrentToNext()); } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void OLk() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void DNN() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
}
