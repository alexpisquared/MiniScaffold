namespace MinNavTpl.VM.VMs;
public partial class Page01VM : BaseDbVM
{
  List<string>? _badEmails;
  int _thisCampaign;
  public Page01VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, 8110)
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
      _badEmails = await DB.QStats.Std.Models.MiscEfDb.GetBadEmails("Select Id from [dbo].[BadEmails]()", Dbx.Database.GetConnectionString() ?? "??");

      PageCvs = CollectionViewSource.GetDefaultView(Dbx.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbx.Emails.ToListAsync());
      PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
      PageCvs.Filter = obj => obj is not Email r || r is null ||
        ((string.IsNullOrEmpty(SearchText) ||
          r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
          r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) &&
        (IncludeClosed == true || (string.IsNullOrEmpty(r.PermBanReason) && _badEmails is not null && !_badEmails.Contains(r.Id))));

      Lgr.Log(LogLevel.Trace, GSReport = $" {PageCvs.Cast<Email>().Count():N0} / {Dbx.Emails.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }

  public EmailOfIStore EmailOfIStore { get; }
  public EmailDetailVM EmailOfIVM { get; }

  [ObservableProperty] Email? currentEmail;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(DelCommand))]
  Email? selectdEmail;
  partial void OnSelectdEmailChanged(Email? value) { if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty

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

  [RelayCommand(CanExecute = nameof(CanDel))] void Del(Email? email) { Bpr.Click(); try { _ = Dbx.Emails.Local.Remove(SelectdEmail!); } catch (Exception ex) { ex.Pop(); } }
  bool CanDel(Email? email) => email is not null; // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand

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
  [RelayCommand] void Nxt() { Bpr.Click(); try { _ = (PageCvs?.MoveCurrentToNext()); } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void OLk() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void DNN() { Bpr.Click(); try { } catch (Exception ex) { ex.Pop(); } }
}
