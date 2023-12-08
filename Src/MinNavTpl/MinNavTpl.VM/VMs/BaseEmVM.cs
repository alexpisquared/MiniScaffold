namespace MinNavTpl.VM.VMs;

public partial class BaseEmVM : BaseDbVM
{
    protected List<string>? _badEmails;
    public BaseEmVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, UserSettings stg, EmailOfIStore eml, EmailDetailVM evm, ISpeechSynth synth, int oid)
        : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, synth, oid)
    {
        EmailOfIStore = eml; //EmailOfIStore.Changed += EmailOfIStore_Chngd;
        EmailOfIVM = evm;
    }

    public async override Task<bool> InitAsync()
    {
        await Task.Delay(22); // <== does not show up without this...............................
        try
        {
            //_thisCampaign = Dbq.Campaigns.Max(r => r.Id);

            _badEmails = await MiscEfDb.GetBadEmails("Select Id from [dbo].[BadEmails]()", Dbq.Database.GetConnectionString() ?? "??");
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }

        return await base.InitAsync();
    }

    public EmailOfIStore EmailOfIStore
    {
        get;
    }
    public EmailDetailVM EmailOfIVM
    {
        get;
    }

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisEmail = "pigida@gmail.com"; partial void OnThisEmailChanged(string value) => ThisFName = GigaHunt.Helpers.FirstLastNameParser.ExtractFirstNameFromEmail(value) ?? ExtractFirstNameFromEmailUsingDb(value) ?? "Sirs";
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisFName = "Oleksa";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(DelCommand))] Email? selectdEmail; partial void OnSelectdEmailChanged(Email? value)
    {
        if (value is not null && _loaded)
        {
            Bpr.Tick();
            UsrStgns.EmailOfI = value.Id;
            EmailOfIStore.Change(value.Id);
            ThisEmail = value.Id;

            _ = Task.Run(async () => await GetDetailsForSelRowAsync(SelectdEmail, Cfg, Dbq)); // _ = Task.Run(GetDetailsForSelRowAsync);
        }
    } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty

    [RelayCommand(CanExecute = nameof(CanSendThis))]
    async Task SendThisAsync()
    {
        GSReport = "";
        await Bpr.StartAsync(8);
        await SendThisOneAsync(ThisEmail);
        await Bpr.FinishAsync(8);
    }
    protected async Task SendThisOneAsync(string email)
    {
        try
        {
            GSReport += $"{email} ... ";

            var timestamp = DateTime.Now;
            var (success, report1) = await QStatusBroadcaster.SendLetter(email, ThisFName, isAvailable: true, timestamp, Lgr);
            if (success)
            {
                var em = Dbq.Emails.FirstOrDefault(r => r.Id == email && r.ReSendAfter != null);
                if (em != null)
                {
                    em.ReSendAfter = null;
                }

                GSReport += "succeeded \r\n";
                _ = await OutlookToDbWindowHelpers.CheckInsert_EMail_EHist_Async(Dbq, email, ThisFName, "", "asu .net 8.0 - success", "ASU - 4 CVs - 2023-12", timestamp, timestamp, "..from std broadcast send", "S");
            }
            else
            {
                _ = await OutlookToDbWindowHelpers.CheckInsert_EMail_EHist_Async(Dbq, email, ThisFName, "", "asu .net 8.0 - FAILURE", "ASU - 4 CVs - 2023-12", timestamp, timestamp, "..from std broadcast send", "S", notes: report1);
                GSReport += $"FAILED ■ ■ ■:  \r\n  {report1} \r\n  ";
                Lgr.Log(LogLevel.Error, GSReport);

                //todo: need logic to inflict the PermaBan on the email address.
            }
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; GSReport += $"FAILED. \r\n  {ex.Message} \r\n"; ex.Pop(Lgr); }
    }
    bool CanSendThis() => !(string.IsNullOrWhiteSpace(ThisEmail) && string.IsNullOrWhiteSpace(ThisFName));

    [RelayCommand(CanExecute = nameof(CanDel))]
    async Task DelAsync(Email? email)
    {
        await Bpr.ClickAsync();
        try
        {
            ArgumentNullException.ThrowIfNull(SelectdEmail, nameof(SelectdEmail));
            var rowsAffected = await Dbq.Emails.Where(r => r.Id == SelectdEmail.Id).ExecuteDeleteAsync(); //tu: delete rows - new ef7 way  <>  old way: _ = dbq.Emails.Local.Remove(email!);
            GSReport = $" {rowsAffected}  rows deleted for \n {SelectdEmail.Id} ";
            _ = Dbq.Emails.Local.Remove(SelectdEmail!); // ?? test saving ??
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }
    static bool CanDel(Email? email) => email is not null; // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand

    [RelayCommand]
    protected void Nxt()
    {
        Bpr.Click(); try { WriteLine(PageCvs?.MoveCurrentToNext()); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [RelayCommand]
    void OLk()
    {
        Bpr.Click(); try { _ = MessageBox.Show("■"); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [RelayCommand]
    void DNN()
    {
        Bpr.Click(); try { _ = MessageBox.Show("■"); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [RelayCommand]
    void PBR()
    {
        Bpr.Click(); try { if (SelectdEmail is null) { return; } SelectdEmail.PermBanReason = $" Not an Agent - {DateTime.Today:yyyy-MM-exMsg}. "; Nxt(); } catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [RelayCommand]
    async Task CouAsync()
    {
        await Bpr.ClickAsync();
        _ = await GetCountryFromWebServiceTaskAsync(SelectdEmail, Cfg);
    }

    [RelayCommand]
    protected async Task GetTopDetailAsync()
    {
        try
        {
            if (PageCvs is null)
            {
                return;
            }

            var curpos = PageCvs.CurrentPosition;

            for (var i = 0; i < 26 && PageCvs?.MoveCurrentToNext() == true; i++)
            {
                await GetDetailsForSelRowAsync(SelectdEmail, Cfg, Dbq);
            }

            if (curpos > 0)
            {
                _ = (PageCvs?.MoveCurrentToPosition(curpos));
            }
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    protected static async Task<string> GetCountryFromWebServiceTaskAsync(Email? email, IConfigurationRoot cfg)
    {
        if (email is null)
        {
            return "";
        }

        try
        {
            if (email.Fname is not null && email.Country is null)
            {
                WriteLine($"■ ■ ■ cfg?[\"WhereAmI\"]: '{new ConfigurationBuilder().AddUserSecrets<Application>().Build()?["WhereAmI"]}'   \t <== ConfigurationBuilder().AddUserSecrets<Application>().");

                ArgumentNullException.ThrowIfNull(cfg, "■▄▀■▄▀■▄▀■▄▀■▄▀■");
                var (_, exMsg, root) = await GenderApi.CallOpenAI(cfg, email.Fname);

                email.Country = root?.country_of_origin.FirstOrDefault()?.country_name ?? root?.errmsg ?? exMsg ?? "?***?";
            }

            return "";
        }
        catch (Exception ex) { ex.Pop(); return $"FAILED. \r\n  {ex.Message}"; }
    }
    protected static async Task GetDetailsForSelRowAsync(Email? email, IConfigurationRoot cfg, QstatsRlsContext dbq)
    {
        if (email is not null && email.Ttl_Sent is null)
        {
            _ = await GetCountryFromWebServiceTaskAsync(email, cfg);

            var rcvds = await dbq.Ehists.CountAsync(r => r.EmailId == email.Id && r.RecivedOrSent == "R");
            if (rcvds > 0)
            {
                email.Ttl_Rcvd = rcvds;
                email.LastRcvd = await dbq.Ehists.Where(r => r.EmailId == email.Id && r.RecivedOrSent == "R").MaxAsync(r => r.EmailedAt);
            }

            var sends = await dbq.Ehists.CountAsync(r => r.EmailId == email.Id && r.RecivedOrSent == "S");
            if (sends > 0)
            {
                email.Ttl_Sent = sends;
                email.LastSent = await dbq.Ehists.Where(r => r.EmailId == email.Id && r.RecivedOrSent == "S").MaxAsync(r => r.EmailedAt);
            }
        }
    }
    string? ExtractFirstNameFromEmailUsingDb(string value) => value; //todo
}