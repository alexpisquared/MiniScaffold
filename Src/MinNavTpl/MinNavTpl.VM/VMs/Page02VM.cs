namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm, ISpeechSynth synth)
      : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, eml, evm, synth, 8110) { }
    public async override Task<bool> InitAsync()
    {
        for (var i = 0; i < 26; i++)
        {
            try
            {
                await Task.Delay(20); // <== does not show up without this...............................
                await Bpr.StartAsync(8);
                IsBusy = true;

                var sw = Stopwatch.StartNew();

                DailyDose = // DevOps.IsDbg ? 2 : 
                    UsrStgns.DailyDose;

                await Dbq.PhoneEmailXrefs.LoadAsync();
                await Dbq.Phones.LoadAsync();

                var emailQuery = // DevOps.IsDbg ? Dbq.Emails.Where(r => IncludeClosed || r.Id.Contains("reply.l") || r.Id.Contains("reply.f") || r.Id.Contains("reply.f")).OrderBy(r => r.LastAction) :
                    Dbq.Emails.Where(r => IncludeClosed || Dbq.VEmailIdAvailProds.Select(r => r.Id).Contains(r.Id) == true).OrderBy(r => r.NotifyPriority);
                await emailQuery.LoadAsync(); //tmi: Lgr.Log(LogLevel.Trace, emailQuery.ToQueryString());

                PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.VEmailAvailProds.ToListAsync());
                                                                                                          //redundant: PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
                PageCvs.Filter = obj => obj is not Email r || r is null || string.IsNullOrEmpty(SearchText) ||
                  r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                  r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

                _ = PageCvs?.MoveCurrentToFirst();
                await GetTopDetailAsync();

                GSReport += $"Emails: {PageCvs?.Cast<Email>().Count():N0} / {sw.Elapsed.TotalSeconds:N1} sec :loaded.";
                Lgr.Log(LogLevel.Trace, $"╞══ Emails: {PageCvs?.Cast<Email>().Count():N0} / {sw.Elapsed.TotalSeconds:N1} sec ");

                if (Environment.GetCommandLineArgs().Contains("Broad") && (DateTimeOffset.Now - DevOps.AppStartedAt).TotalSeconds < antiSpamSec)
                {
                    await SendTopNAsync();
                }

                if (Clipboard.ContainsText() && Clipboard.GetText().Length < 48)
                {
                    if (RegexHelper.IsEmail(Clipboard.GetText()))
                    {
                        ThisEmail = Clipboard.GetText();
                    }
                    else
                    {
                        SearchText = Clipboard.GetText();
                    }
                }

                await Bpr.FinishAsync(8);
                return await base.InitAsync();
            }
            catch (SqlException ex) { GSReport += $"FAILED {i,2}/{26}  {ex.Message}\n"; Synth.SpeakFAF($"Retrying {25 - i} times."); }
            catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}\n"; ex.Pop(Lgr); return false; }
            finally { IsBusy = false; }
        } // for

        return false;
    }

    [ObservableProperty] int dailyDose; partial void OnDailyDoseChanged(int value)
    {
        if (_loaded) { UsrStgns.DailyDose = value; }
    }
    [ObservableProperty][NotifyPropertyChangedFor(nameof(GSReport))] ObservableCollection<Email> selectedEmails = []; partial void OnSelectedEmailsChanged(ObservableCollection<Email> value)
    {
        GSReport += $"{value.Count,4} / {SelectedEmails.Count} rows selected"; ;
    }

    [RelayCommand]
    async Task SendTopNAsync()
    {
        GSReport += $"\nSending top {DailyDose} emails; anti spam pause is {antiSpamSec} seconds ...\n... See you in {(antiSpamSec + 5) * (DailyDose - 1) / 60.0:N0} minutes.\n";
        await Synth.SpeakFreeAsync($"Sending top {DailyDose} emails; anti spam pause is {antiSpamSec} seconds ... See you in {(antiSpamSec + 5) * (DailyDose - 1) / 60.0:N0} minutes.", speakingRate1010: 4, volumePercent: 26);

        var start = Stopwatch.GetTimestamp();
        var i = 0;
        foreach (Email email in PageCvs ?? throw new ArgumentNullException("ex21: main page list is still NUL"))
        {
            if (++i > DailyDose) { break; }

            await SendAndReportOneAsync(i, DailyDose, email);
        }

        await FinishJobAsync(Stopwatch.GetElapsedTime(start), i - 1);
    }

    [RelayCommand]
    async Task SendSlctAsync()
    {
        GSReport += $"\nSending top {SelectedEmails.Count} emails; anti spam pause is {antiSpamSec} seconds ...\n... See you in {(antiSpamSec + 5) * (SelectedEmails.Count - 1) / 60.0:N0} minutes.\n";
        await Synth.SpeakFreeAsync($"Sending top {SelectedEmails.Count} emails; anti spam pause is {antiSpamSec} seconds ... See you in {(antiSpamSec + 5) * (SelectedEmails.Count - 1) / 60.0:N0} minutes.", speakingRate1010: 4, volumePercent: 26);

        var start = Stopwatch.GetTimestamp();
        var i = 0;
        foreach (var email in SelectedEmails ?? throw new ArgumentNullException("ex32: selected emails collection is still NUL"))
        {
            await SendAndReportOneAsync(++i, SelectedEmails.Count, email);
        }

        await FinishJobAsync(Stopwatch.GetElapsedTime(start), i);
    }

    async Task SendAndReportOneAsync(int i, int j, Email email)
    {
        if (i > 1) await Task.Delay(antiSpamSec * 1000 + 100);

        GSReport += $"{DateTime.Now:HH:mm:ss.f} {i,5} / {j} \t";
        await SendThisOneAsync(email.Id, email.Fname, i);
        Synth.SpeakFreeFAF($"{i} down, {j - i} to go...", speakingRate1010: 6, volumePercent: 9);
    }
    async Task FinishJobAsync(TimeSpan totalTime, int i)
    {
        Lgr.Log(LogLevel.Information, $"│  {totalTime,9:m\\:ss\\.fff}  /  {(i - 1)}  =  {(i <= 1 ? TimeSpan.Zero : totalTime / (i - 1)):m\\:ss\\.fff} sec/email");
        await Bpr.FinishAsync(8);
        var prev = GSReport;
        await Synth.SpeakAsync($"Running Outlook-to-DB now (to avoid double sending!) ...", volumePercent: 3);
        MainVM.NavBarVM.NavigatePage03Command.Execute(null); //tu: ad hoc navigation
        GSReport = prev;
    }

    readonly int antiSpamSec = DevOps.IsDbg ? 5 : 118; // 2 min to better tell apart by the timestamps.
}