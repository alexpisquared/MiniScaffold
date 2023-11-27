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
                .Where(r => Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id))
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
        catch (Exception ex) { ex.Pop(Lgr); return false; }
        finally { rv = await base.InitAsync(); }

        return rv;
    }

    [ObservableProperty] int topNumber = 10;
    [ObservableProperty] string thisEmail = "pigida@gmail.com";
    [ObservableProperty] string thisFName = "Oleksa";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] Email? currentEmail; // demo only.
    [ObservableProperty] Email? selectdEmail; partial void OnSelectdEmailChanged(Email? value)
    {
        if (value is not null && _loaded) { Bpr.Tick(); UsrStgns.EmailOfI = value.Id; EmailOfIStore.Change(value.Id); }
    } // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] ObservableCollection<Email> selectedEmails = []; partial void OnSelectedEmailsChanged(ObservableCollection<Email> value)
    {
        GSReport = $"//todo: {value.Count:N0}  rows selected"; ;
    }

    [RelayCommand]
    void SendTopN()
    {
        GSReport = $"//todo: Sendging top {TopNumber} ..."; ;
    }
    [RelayCommand]
    void SendSlct()
    {
        GSReport = $"//todo: Sendging {SelectedEmails.Count} selects"; ;
        ; ;
    }
    [RelayCommand]
    async Task SendThisAsync()
    {
        try
        {
            GSReport = $"Sending to {ThisEmail}...";

            var firstName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase((ThisFName ?? "Sirs").ToLower()); // ALEX will be ALEX without .ToLower() (2020-12-03)

            var timestamp = DateTime.Now;
            var (success, report) = await QStatusBroadcaster.SendLetter(ThisEmail, firstName, isAvailable: true, timestamp);
            if (success)
            {
                var em = Dbq.Emails.FirstOrDefault(r => r.Id == ThisEmail && r.ReSendAfter != null);
                if (em != null)
                {
                    em.ReSendAfter = null;
                }

                _ = await OutlookToDbWindowHelpers.CheckInsert_EMail_EHist_Async(Dbq, ThisEmail, firstName, "", "ASU Subj", "ASU Body + 4 CVs", timestamp, timestamp, "..from std broadcast send", "S");
            }
            else
            {
                var em = Dbq.Emails.FirstOrDefault(r => r.Id == ThisEmail);
                if (em != null)
                {
                    em.PermBanReason = $"Err: {report}   {em.PermBanReason}";
                }
            }

            var (_, _, report2) = await Dbq.TrySaveReportAsync();

            GSReport = $"Sending to {ThisEmail}... done.   {report2}";
        }
        catch (Exception ex) { GSReport = $"Sending to {ThisEmail}... failed.   {ex.Message}"; ex.Pop(Lgr); }
        finally { }
    }
}