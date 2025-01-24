namespace MinNavTpl.VM.VMs;
public partial class BaseEmVM : BaseDbVM
{
    bool _limitReached;
    protected List<string>? _badEmails;
    public BaseEmVM(ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, LetDbChgStore awd, IsBusy__Store bzi, UserSettings stg, EmailOfIStore eml, EmailDetailVM evm, ISpeechSynth synth, int oid) : base(lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, bzi, stg, synth, oid)
    {
        EmailOfIStore = eml; //EmailOfIStore.Changed += EmailOfIStore_Chngd;
        EmailOfIVM = evm;
    }

    public async override Task<bool> InitAsync()
    {
        await Task.Delay(22); // <== does not show up without this...............................

        return await base.InitAsync();
    }
    public override Task<bool> WrapAsync()
    {
        _ = EmailOfIVM.WrapAsync();
        return base.WrapAsync();
    }

    public EmailOfIStore EmailOfIStore
    {
        get;
    }
    public EmailDetailVM EmailOfIVM
    {
        get;
    }
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisEmail = "pigida@gmail.com"; partial void OnThisEmailChanged(string value) => ThisFName = FirstLastNameParser.ExtractFirstNameFromEmail(value) ?? ExtractFirstNameFromEmailUsingDb(value) ?? "Sirs";
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisFName = "Sir/Madam";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(DelCommand))] Email? selectdEmail; async partial void OnSelectdEmailChanged(Email? value)
    {
        if (value is null || !_loaded) return;

        //Bpr.Tick();
        UsrStgns.EmailOfI = value.Id;
        EmailOfIStore.Change(value.Id);
        ThisEmail = value.Id;

        if (((ListCollectionView?)PageCvs)?.ItemProperties[1].Name == "PhoneNumber") // Page07VM uses this property for the phone list - not the email list => skip the email row details fetching.
        {
            return;
        }

        await GetDetailsForSelRowAsync(value, Cfg, Dbq);
        PageCvs?.Refresh(); //prevents from using up/down arrows to navigate thru the list.
    } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty

    [RelayCommand] async Task SendEmailPocAsync() => await SendThisOneAsync("pigida@gmail.com", "Oleksa", 1);

    [RelayCommand(CanExecute = nameof(CanSendThis))]
    async Task SendThisAsync()
    {
        await Bpr.StartAsync(8); await SendThisOneAsync(ThisEmail, ThisFName, 1); await Bpr.FinishAsync(8);
    }
    protected async Task SendThisOneAsync(string email, string? rawName, int count)
    {
        try
        {
            var fName = string.IsNullOrEmpty(rawName)
                ? (FirstLastNameParser.ExtractFirstNameFromEmail(email) ?? ExtractFirstNameFromEmailUsingDb(email) ?? "Sirs")
                : FirstLastNameParser.ToTitleCase(rawName);

            GSReport += $"{fName,-15}\t{email,-47}";

            var now = DateTime.Now;
            var (success, report1) = await QStatusBroadcaster.SendLetter(email, fName, isAvailable: true, now, Lgr);
            if (success)
            {
                var em = Dbq.Emails.FirstOrDefault(r => r.Id == email && r.ReSendAfter != null);
                if (em != null)
                {
                    em.ReSendAfter = null;
                }

                Lgr.Log(LogLevel.Information, $"│  :sent/timestamped:  {now:MMddHHmmss} {count,3}  {fName,-18}{email}");

                GSReport += "\n";
                _ = await new OutlookToDbWindowHelpers(Lgr).CheckInsert_EMail_EHist_Async(Dbq, email, fName, "", "asu .net 8.0 - success", "ASU - 4 CVs - 2023-12", now, now, "..from std broadcast send", "S");
            }
            else
            {
                GSReport += $"\t ■ ■ ■ {report1} \n";
                if (!VersionHelper.IsDbg) await Synth.SpeakAsync(report1, volumePercent: 100);
            }
        }
        catch (Exception ex) { GSReport += $"FAILED  {ex.Message} \n"; ex.Pop(Lgr); }
    }
    bool CanSendThis() => !(string.IsNullOrWhiteSpace(ThisEmail) && string.IsNullOrWhiteSpace(ThisFName));

    [RelayCommand(CanExecute = nameof(CanSaveThis))]
    async Task SaveThisAsync()
    {
        await Bpr.StartAsync(8); await SaveThisOneAsync(ThisEmail, ThisFName, 1); await Bpr.FinishAsync(8);
    }
    protected async Task SaveThisOneAsync(string email, string? rawName, int count)
    {
        try
        {
            var fName = string.IsNullOrEmpty(rawName)
                ? (FirstLastNameParser.ExtractFirstNameFromEmail(email) ?? ExtractFirstNameFromEmailUsingDb(email) ?? "Sirs")
                : FirstLastNameParser.ToTitleCase(rawName);

            var (email1, isNew) = await new OutlookToDbWindowHelpers(Lgr).CheckInsertEMailAsync(Dbq, email, fName, "", /*$"..from body (sender: {originalSenderEmail}). "*/null, DateTime.Now, false);
            GSReport += $"{fName,-15}\t{email,-47}   {(isNew == false ? "already exists" : isNew == true ? "saved" : "failed to save somehow?...")}\n";
        }
        catch (Exception ex) { GSReport += $"FAILED  {ex.Message} \n"; ex.Pop(Lgr); }
    }
    bool CanSaveThis() => !(string.IsNullOrWhiteSpace(ThisEmail) && string.IsNullOrWhiteSpace(ThisFName));

    [RelayCommand(CanExecute = nameof(CanDel))]
    async Task DelAsync(Email? email)
    {
        await Bpr.ClickAsync();
        try
        {
            ArgumentNullException.ThrowIfNull(email, nameof(email));
            //var rowsAffected = await Dbq.Emails.Where(r => r.Id == email.Id).ExecuteDeleteAsync(); //tu: delete rows - new ef7 way  <>  old way: _ = dbq.Emails.Local.Remove(email!);
            //GSReport += $" {rowsAffected}  rows deleted for \n {email.Id} ";

            foreach (var ehist in Dbq.Ehists.Where(r => r.EmailId == email.Id)) { _ = Dbq.Ehists.Remove(ehist); } // no .Local. to save on preloading the whole table into memory.

            foreach (var phnxr in Dbq.PhoneEmailXrefs.Local.Where(r => r.EmailId == email.Id)) { _ = Dbq.PhoneEmailXrefs.Local.Remove(phnxr); }

            _ = Dbq.Emails.Local.Remove(email!);
        }
        catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }
    static bool CanDel(Email? email) => email is not null; // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand

    [RelayCommand]
    protected void Nxt()
    {
        Bpr.Click(); try { WriteLine(PageCvs?.MoveCurrentToNext()); } catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }

    [RelayCommand]
    void OLk()
    {
        Bpr.Click(); try { _ = MessageBox.Show("■"); } catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }

    [RelayCommand]
    void DNN()
    {
        Bpr.Click();
        ArgumentNullException.ThrowIfNull(SelectdEmail);
        SelectdEmail.DoNotNotifyOnAvailableForCampaignId = _thisCampaignId;
        SelectdEmail.ModifiedAt = DateTime.Now;
        Nxt();
    }

    [RelayCommand]
    void PBR()
    {
        Bpr.Click();
        try
        {
            if (SelectdEmail is null) { return; }

            SelectdEmail.PermBanReason = $" Not an Agent - {DateTime.Today:yyyy-MM-dd}. ";
            SelectdEmail.ModifiedAt = DateTime.Now;
            Nxt();
        }
        catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }

    [RelayCommand]
    async Task CouAsync()
    {
        await Bpr.ClickAsync();
        _ = await SetCountryFromWebServiceTaskAsync(SelectdEmail, Cfg);
    }

    [RelayCommand]
    protected async Task GetTopDetailAsync(int topRows = 8)
    {
        try
        {
            if (PageCvs is null) return;

            var prevPos = PageCvs.CurrentPosition;

            for (var i = 0; i < topRows && PageCvs?.MoveCurrentToNext() == true; i++)
            {
                await GetDetailsForSelRowAsync(SelectdEmail, Cfg, Dbq);
            }

            _ = (PageCvs?.MoveCurrentToPosition(prevPos));
            await GetDetailsForSelRowAsync(SelectdEmail, Cfg, Dbq);

            PageCvs?.Refresh();
        }
        catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }

    protected async Task<string> SetCountryFromWebServiceTaskAsync(Email email, IConfigurationRoot cfg)
    {
        if (string.IsNullOrEmpty(email.Fname)) return GenderApiConst.Retries[5];

        if (_limitReached)
        {
            if (string.IsNullOrEmpty(email.Country) || GenderApiConst.Retries.Contains(email.Country)/* && email.Country != GenderApiConst.LimitReachedOld*/)
            {
                await ReuseAsync(email, cfg, GenderApiConst.Retries, true);
            }

            return GenderApiConst.LimitReachedNew;
        }

        try
        {
            if (email.Fname is not null && (string.IsNullOrEmpty(email.Country) || GenderApiConst.Retries.Contains(email.Country)))// || System.Text.RegularExpressions.Regex.IsMatch(email.Fname, @"[0-9\W]"))) // check if the email.Fname contains a number or a special character using regex
            {
                ArgumentNullException.ThrowIfNull(cfg, "■▄▀■▄▀■▄▀■▄▀■▄▀■");
                var (_, exMsg, root) = await GenderApi.CallGenderApi(cfg, email.Fname, Synth);

                await ReuseAsync(email, cfg, GenderApiConst.Retries, false);

                if (GenderApiConst.IsLimitReached(email.Country))
                {
                    _limitReached = true;
                }
            }

            return "";
        }
        catch (Exception ex) { ex.Pop(Lgr); return $"FAILED. \n  {ex.Message}"; }
    }

    async Task ReuseAsync(Email email, IConfigurationRoot cfg, string[] retries, bool cacheOnly)
    {
        var (_, exMsg, root) = await GenderApi.CallGenderApi(cfg, email.Fname, Synth, cacheOnly: cacheOnly);
        var newCountry =
            root is null ? (exMsg ?? retries[2]) :
            root?.country_of_origin.FirstOrDefault() is null ? (root?.errmsg ?? exMsg ?? retries[0]) :
            root?.country_of_origin.First().country_name ?? root?.errmsg ?? exMsg ?? retries[1];

        if (email.Country != newCountry)
        {
            email.Country = newCountry;
            email.ModifiedAt = DateTime.Now;
        }
    }

    protected async Task GetDetailsForSelRowAsync(Email? email, IConfigurationRoot cfg, QstatsRlsContext dbq)
    {
        if (email is null) // ??? || email.Ttl_Sent is not null)
        {
            return;
        }

        _ = await SetCountryFromWebServiceTaskAsync(email, cfg);

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
    string? ExtractFirstNameFromEmailUsingDb(string value) => value; //todo
}