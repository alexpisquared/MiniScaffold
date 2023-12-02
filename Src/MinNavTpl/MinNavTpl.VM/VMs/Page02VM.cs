namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm)
      : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, eml, evm, 8110) { }
    public async override Task<bool> InitAsync()
    {
        var rv = false;
        try
        {
            IsBusy = true;
            await Bpr.StartAsync(8);

            var sw = Stopwatch.StartNew();

            await Dbq.PhoneEmailXrefs.LoadAsync();
            await Dbq.Phones.LoadAsync();
            await Dbq.Emails
                .Where(r => Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id) != DevOps.IsDbg)
                .OrderBy(r => r.NotifyPriority)
                .LoadAsync();

            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());
            //redundant: PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Email r || r is null || string.IsNullOrEmpty(SearchText) ||
              r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
              r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            Lgr.Log(LogLevel.Trace, GSReport = $"//todo: ({Dbq.VEmailAvailProds.Local.Count:N0} + {Dbq.VEmailAvailProds.Local.Count:N0} / {sw.Elapsed.TotalSeconds:N1} loaded rows / s");

            await Bpr.FinishAsync(8);
        }
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { rv = await base.InitAsync(); }

        return rv;
    }

    [ObservableProperty] int topNumber = 10;
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
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] ObservableCollection<Email> selectedEmails = []; partial void OnSelectedEmailsChanged(ObservableCollection<Email> value)
    {
        GSReport = $"//todo: {value.Count:N0}  rows selected"; ;
    }

    [RelayCommand]
    async Task SendTopNAsync()
    {
        GSReport = $"...";
        await Bpr.StartAsync(8);

        var i = 0;
        foreach (Email email in PageCvs ?? throw new ArgumentNullException("@#@#@#@#!@#@!"))
        {
            if (++i >= TopNumber)
            {
                break;
            }

            await SendThisOneAsync(email.Id);
        }

        await Bpr.FinishAsync(8);
    }
    [RelayCommand]
    async Task SendSlctAsync()
    {
        GSReport = $"...";
        await Bpr.StartAsync(8);

        foreach (var email in SelectedEmails ?? throw new ArgumentNullException("@#@#@#@#!@#@!"))
        {
            await SendThisOneAsync(email.Id);
        }

        await Bpr.FinishAsync(8);
    }
    [RelayCommand(CanExecute = nameof(CanSendThis))]
    async Task SendThisAsync()
    {
        GSReport = $"...";
        await Bpr.StartAsync(8);
        await SendThisOneAsync(ThisEmail);
        await Bpr.FinishAsync(8);
    }

    async Task SendThisOneAsync(string email)
    {
        try
        {
            GSReport += $"Sending to {email}... ";

            var timestamp = DateTime.Now;
            var (success, report1) = await QStatusBroadcaster.SendLetter(email, ThisFName, isAvailable: true, timestamp, Lgr);
            if (success)
            {
                var em = Dbq.Emails.FirstOrDefault(r => r.Id == email && r.ReSendAfter != null);
                if (em != null)
                {
                    em.ReSendAfter = null;
                }

                GSReport += "succeeded";
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
        catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); }
    }

    bool CanSendThis() => !(string.IsNullOrWhiteSpace(ThisEmail) && string.IsNullOrWhiteSpace(ThisFName));

    string? ExtractFirstNameFromEmailUsingDb(string value) => value; //todo
}