namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm, ISpeechSynth synth)
      : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, eml, evm, 8110)
    {
        Synth = synth;
    }
    public async override Task<bool> InitAsync()
    {
        var rv = false;
        try
        {
            IsBusy = true;
            await Bpr.StartAsync(8);
            await Task.Delay(2); // <== does not show up without this...............................
            rv = await base.InitAsync(); _loaded = false; IsBusy = true; // or GSReport does not work (store is not ready yet?)...

            var sw = Stopwatch.StartNew();

            await Dbq.PhoneEmailXrefs.LoadAsync();
            await Dbq.Phones.LoadAsync();

            var emailQuery = DevOps.IsDbg ?
                Dbq.Emails.Where(r => r.Id.Contains("reply.l") || r.Id.Contains("reply.f") || r.Id.Contains("reply.f")).OrderBy(r => r.LastAction) :
                Dbq.Emails.Where(r => Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id) == true).OrderBy(r => r.NotifyPriority);

            Lgr.Log(LogLevel.Trace, emailQuery.ToQueryString());

            await emailQuery.LoadAsync();

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());
            //redundant: PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Email r || r is null || string.IsNullOrEmpty(SearchText) ||
              r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            Lgr.Log(LogLevel.Trace, GSReport = $" Emails: {PageCvs?.Cast<Email>().Count():N0} cvs / {Dbq.Emails.Local.Count:N0} local / {sw.Elapsed.TotalSeconds:N1} sec ");

            if (Environment.GetCommandLineArgs().Contains("Broad") && (DateTimeOffset.Now - DevOps.AppStartedAt).TotalSeconds < antiSpamSec)
            {
                await SendTopNAsync();
            }

            await Bpr.FinishAsync(8);
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { rv = await base.InitAsync(); }

        return rv;
    }

    [ObservableProperty] int topNumber = DevOps.IsDbg ? 2 : 5;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisEmail = "pigida@gmail.com"; partial void OnThisEmailChanged(string value) => ThisFName = GigaHunt.Helpers.FirstLastNameParser.ExtractFirstNameFromEmail(value) ?? ExtractFirstNameFromEmailUsingDb(value) ?? "Sirs";
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendThisCommand))] string thisFName = "Oleksa";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.
    [ObservableProperty] Email? selectdEmail; partial void OnSelectdEmailChanged(Email? value)
    {
        if (!(value is not null && _loaded)) return;

        Bpr.Tick();
        UsrStgns.EmailOfI = value.Id;
        EmailOfIStore.Change(value.Id);

        ThisEmail = value.Id;

    } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] ObservableCollection<Email> selectedEmails = [];

    public ISpeechSynth Synth
    {
        get;
    }

    partial void OnSelectedEmailsChanged(ObservableCollection<Email> value)
    {
        GSReport = $"//todo: {value.Count:N0}  rows selected"; ;
    }

    [RelayCommand]
    async Task SendTopNAsync()
    {
        GSReport = $"";
        await Synth.SpeakAsync($"Sending top {TopNumber} emails; anti spam pause is {antiSpamSec} seconds ...");
        await Bpr.StartAsync(8);

        var i = 0;
        foreach (Email email in PageCvs ?? throw new ArgumentNullException("ex21: main page list is still NUL"))
        {
            if (++i > TopNumber)
            {
                break;
            }

            GSReport += $"{i} / {TopNumber}  ";
            await Task.Delay(antiSpamSec * 1000);
            await SendThisOneAsync(email.Id);
            await Synth.SpeakAsync($"{i} down, {TopNumber - i} to go...");
        }

        await Bpr.FinishAsync(8);
        await Synth.SpeakAsync($"Running Outlook-to-DB now (to avoid double sending!) ...");
        GSReport += "\n\t!!! MUST RUN OUTLOOK --> DB SYNC NOW !!!";
        MainVM.NavBarVM.NavigatePage03Command.Execute(null); //tu: ad hoc navigation
    }

    [RelayCommand]
    async Task SendSlctAsync()
    {
        GSReport = $"";
        await Synth.SpeakAsync($"Sending selected emails; anti spam pause is {antiSpamSec} seconds ...");
        await Bpr.StartAsync(8);

        foreach (var email in SelectedEmails ?? throw new ArgumentNullException("ex32: selected emails collection is still NUL"))
        {
            await SendThisOneAsync(email.Id);
            await Task.Delay(antiSpamSec * 1000);
        }

        await Bpr.FinishAsync(8);
        await Synth.SpeakAsync($"Running Outlook-to-DB now (to avoid double sending!) ...");
        GSReport += "\n\t!!! MUST RUN OUTLOOK --> DB SYNC NOW !!!";
        MainVM.NavBarVM.NavigatePage03Command.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(CanSendThis))]
    async Task SendThisAsync()
    {
        GSReport = "";
        await Bpr.StartAsync(8);
        await SendThisOneAsync(ThisEmail);
        await Bpr.FinishAsync(8);
    }

    async Task SendThisOneAsync(string email)
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

    string? ExtractFirstNameFromEmailUsingDb(string value) => value; //todo
    readonly int antiSpamSec = DevOps.IsDbg ? 5 : 60;
}