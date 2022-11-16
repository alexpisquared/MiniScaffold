namespace MinNavTpl.VM.VMs;

public partial class BaseEmVM : BaseDbVM
{
  protected List<string>? _badEmails;
  int _thisCampaign;
  public BaseEmVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QstatsRlsContext dbx, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, UserSettings stg, EmailOfIStore eml, EmailDetailVM evm, int oid) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, gsr, awd, stg, oid)
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

  [RelayCommand(CanExecute = nameof(CanDel))]
  void Del(Email? email)
  {
    Bpr.Click(); try
    {
      _ = Dbx.Emails.Local.Remove(SelectdEmail!);
      //var rowsAffected = await Dbx.Emails.Where(r=> r.Id == selectdEmail.Id).ExecuteDeleteAsync(); //tu: delete rows - new ef7 way.
    }
    catch (Exception ex) { ex.Pop(); }
  }
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
  [RelayCommand] void Nxt() { Bpr.Click(); try { WriteLine(PageCvs?.MoveCurrentToNext()); } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void OLk() { Bpr.Click(); try { MessageBox.Show("■"); } catch (Exception ex) { ex.Pop(); } }
  [RelayCommand] void DNN() { Bpr.Click(); try { MessageBox.Show("■"); } catch (Exception ex) { ex.Pop(); } }
}
