namespace MinNavTpl.VM.VMs;
public partial class Page01VM : BaseEmVM
{
    public Page01VM( ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, IsBusy__Store bzi, EmailDetailVM evm, ISpeechSynth synth)
      : base(lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, bzi, stg, eml, evm, synth, 8110) { }
    public async override Task<bool> InitAsync()
    {
        try
        {
            await Task.Delay(20); // <== does not show up without this...............................
            await Bpr.StartAsync(8);
            IsBusy = true;

            var sw = Stopwatch.StartNew();

            await Dbq.PhoneEmailXrefs.LoadAsync();
            await Dbq.Phones.LoadAsync();

            _badEmails = await MiscEfDb.GetBadEmails("Select Id from [dbo].[BadEmails]()", Dbq.Database.GetConnectionString() ?? "??");

            await Dbq.Emails.LoadAsync();
            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Emails.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await dbq.Emails.ToListAsync());
            PageCvs.SortDescriptions.Add(new SortDescription(nameof(Email.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Email r || r is null ||
              ((string.IsNullOrEmpty(SearchText) ||
                r.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) &&
              (IncludeClosed == true || (string.IsNullOrEmpty(r.PermBanReason) && _badEmails is not null && !_badEmails.Contains(r.Id))));

            GSReport += $"Emails: {PageCvs?.Cast<Email>().Count():N0} good / {Dbq.Emails.Local.Count:N0} total / {sw.Elapsed.TotalSeconds:N1} sec \n";
            Lgr.Log(LogLevel.Trace, GSReport);

            if (Clipboard.ContainsText() && Clipboard.GetText().Trim().Length < 16)
            {
                SearchText = Clipboard.GetText().Trim();
            }

            _ = PageCvs?.MoveCurrentToFirst();
            await GetTopDetailAsync(52);

            await Bpr.FinishAsync(8);
            return await base.InitAsync();
        }
        catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); return false; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    void AddNewEmail()
    {
        try { var newEml = new Email { AddedAt = DateTime.Now, Notes = string.IsNullOrEmpty(Clipboard.GetText().Trim()) ? "New Email" : Clipboard.GetText().Trim() }; Dbq.Emails.Local.Add(newEml); SelectdEmail = newEml; } catch (Exception ex) { GSReport += $"FAILED. \n  {ex.Message}"; ex.Pop(Lgr); }
    }
}