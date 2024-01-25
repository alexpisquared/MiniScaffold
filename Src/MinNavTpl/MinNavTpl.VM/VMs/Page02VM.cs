﻿namespace MinNavTpl.VM.VMs;
public partial class Page02VM : BaseEmVM
{
    public Page02VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, EmailDetailVM evm, ISpeechSynth synth)
      : base(mvm, lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg, eml, evm, synth, 8110) { }
    public async override Task<bool> InitAsync()
    {
        for (int i = 0; i < 26; i++)
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

                GSReport = $"╞══ Emails: {PageCvs?.Cast<Email>().Count():N0} / {sw.Elapsed.TotalSeconds:N1} sec ";
                Lgr.Log(LogLevel.Trace, GSReport);

                if (Environment.GetCommandLineArgs().Contains("Broad") && (DateTimeOffset.Now - DevOps.AppStartedAt).TotalSeconds < antiSpamSec)
                {
                    await SendTopNAsync();
                }

                await Bpr.FinishAsync(8);
                return await base.InitAsync();
            }
            catch (SqlException ex) { GSReport = $"FAILED {i,2}/{26}  {ex.Message}"; Synth.SpeakFAF("Retrying many times."); }
            catch (Exception ex) { GSReport = $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; }
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
        GSReport = $"//todo: {value.Count:N0}  rows selected"; ;
    }

    [RelayCommand]
    async Task SendTopNAsync()
    {
        GSReport = $"";
        await Synth.SpeakAsync($"Sending top {DailyDose} emails; anti spam pause is {antiSpamSec} seconds ... See you in {/*DateTime.Now.AddSeconds*/(antiSpamSec + 5) * DailyDose / 60.0:N0} minutes.");
        await Bpr.StartAsync(8);

        var i = 0;
        foreach (Email email in PageCvs ?? throw new ArgumentNullException("ex21: main page list is still NUL"))
        {
            if (++i > DailyDose)
            {
                break;
            }

            GSReport += $"{i,3} / {DailyDose}\t";
            await Task.Delay(antiSpamSec * 1000);
            await SendThisOneAsync(email.Id, email.Fname);
            await Synth.SpeakAsync($"{i} down, {DailyDose - i} to go...", volumePercent: 3);
        }

        await Bpr.FinishAsync(8);
        await Synth.SpeakAsync($"Running Outlook-to-DB now (to avoid double sending!) ...", volumePercent: 3);
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
            await SendThisOneAsync(email.Id, email.Fname);
            await Task.Delay(antiSpamSec * 1000);
        }

        await Bpr.FinishAsync(8);
        await Synth.SpeakAsync($"Running Outlook-to-DB now (to avoid double sending!) ...", volumePercent: 3);
        GSReport += "\n\t!!! MUST RUN OUTLOOK --> DB SYNC NOW !!!";
        MainVM.NavBarVM.NavigatePage03Command.Execute(null);
    }

    readonly int antiSpamSec = DevOps.IsDbg ? 5 : 80;
}