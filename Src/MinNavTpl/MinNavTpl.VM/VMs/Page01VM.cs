namespace MinNavTpl.VM.VMs;
public partial class Page01VM : BaseEmVM
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

  [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.
  [ObservableProperty][NotifyCanExecuteChangedFor(nameof(DelCommand))] Email? selectdEmail; partial void OnSelectdEmailChanged(Email? value) { if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); } } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty

  [RelayCommand(CanExecute = nameof(CanDel))] void Del(Email? email) { Bpr.Click(); try { _ = Dbx.Emails.Local.Remove(SelectdEmail!); } catch (Exception ex) { ex.Pop(); } }            //var rowsAffected = await Dbx.Emails.Where(r=> r.Id == selectdEmail.Id).ExecuteDeleteAsync(); //tu: delete rows - new ef7 way.
  bool CanDel(Email? email) => email is not null; // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand
  [RelayCommand] void PBR() { Bpr.Click(); try { if (SelectdEmail is null) return; SelectdEmail.PermBanReason = $" Not an Agent - {DateTime.Today:yyyy-MM-dd}. "; Nxt(); } catch (Exception ex) { ex.Pop(); } }
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
        var (ts, dd, root) = await GenderApi.CallOpenAI(Cfg, SelectdEmail.Fname);

        SelectdEmail.Country = root?.country_of_origin.FirstOrDefault()?.country_name ?? root?.errmsg ?? dd ?? "?***?";
      }
    }
    catch (Exception ex) { ex.Pop(); }
  }
  [RelayCommand]
  async void GetTopDetail()
  {
    Bpr.Start(8);
    try
    {
      //_ = (PageCvs?.MoveCurrentToFirst());
      for (var i = 0; i < 25; i++)
      {
        if (PageCvs?.MoveCurrentToNext() == true && SelectdEmail is not null)
        {
          var (ts, dd, root) = await GenderApi.CallOpenAI(Cfg, SelectdEmail.Fname ?? throw new ArgumentNullException(), true);

          SelectdEmail.Country = root?.country_of_origin.FirstOrDefault()?.country_name ?? root?.errmsg ?? dd ?? "?***?";
          SelectdEmail.Ttl_Rcvd = await Dbx.Ehists.CountAsync(r => r.EmailId == SelectdEmail.Id && r.RecivedOrSent == "R");
          SelectdEmail.Ttl_Sent = await Dbx.Ehists.CountAsync(r => r.EmailId == SelectdEmail.Id && r.RecivedOrSent == "S");
          if (SelectdEmail.Ttl_Rcvd > 0) SelectdEmail.LastRcvd = await Dbx.Ehists.Where(r => r.EmailId == SelectdEmail.Id && r.RecivedOrSent == "R").DefaultIfEmpty().MaxAsync(r => r.EmailedAt);
          if (SelectdEmail.Ttl_Sent > 0) SelectdEmail.LastSent = await Dbx.Ehists.Where(r => r.EmailId == SelectdEmail.Id && r.RecivedOrSent == "S").DefaultIfEmpty().MaxAsync(r => r.EmailedAt);
        }
      }
      //_ = (PageCvs?.MoveCurrentToFirst());
      Bpr.Finish(8);
    }
    catch (Exception ex) { ex.Pop(); }
  }
}