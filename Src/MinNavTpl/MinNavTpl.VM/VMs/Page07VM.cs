namespace MinNavTpl.VM.VMs;
public partial class Page07VM : BaseEmVM
{
    public Page07VM( ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecurityForcer sec, QstatsRlsContext dbq, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, GSReportStore gsr, EmailOfIStore eml, LetDbChgStore awd, IsBusy__Store bzi, EmailDetailVM evm, ISpeechSynth synth)
//    : base(lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, stg,           synth, 8990) { }
      : base(lgr, cfg, bpr, sec, dbq, win, svr, dbs, gsr, awd, bzi, stg, eml, evm, synth, 8880) { }
    public async override Task<bool> InitAsync()
    {
        try
        {
            await Task.Delay(20); // <== does not show up without this...............................
            await Bpr.StartAsync(8);
            IsBusy = true;

            var sw = Stopwatch.StartNew();

            await Dbq.PhoneEmailXrefs.LoadAsync();
            await Dbq.Emails.LoadAsync();

            await Dbq.Phones.LoadAsync();
            PageCvs = CollectionViewSource.GetDefaultView(Dbq.Phones.Local.ToObservableCollection()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.Phones.ToListAsync());
            PageCvs.SortDescriptions.Add(new SortDescription(nameof(Phone.AddedAt), ListSortDirection.Descending));
            PageCvs.Filter = obj => obj is not Phone r || r is null ||
              ((string.IsNullOrEmpty(SearchText) ||
                r.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) // && (IncludeClosed == true)
              );

            _ = PageCvs?.MoveCurrentToFirst();

            GSReport += $"├── Phones: {PageCvs?.Cast<Phone>().Count():N0} good / {Dbq.Phones.Local.Count:N0} total / {sw.Elapsed.TotalSeconds:N1} sec \n";
            Lgr.Log(LogLevel.Trace, GSReport);

            SelectdPhone = null;

            await Bpr.FinishAsync(8);
            return await base.InitAsync();
        } catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(Lgr); return false; } finally { IsBusy = false; }
    }

    [RelayCommand]
    void AddNewPhone()
    {
        try { var newEml = new Phone { AddedAt = DateTime.Now, Notes = string.IsNullOrEmpty(Clipboard.GetText().Trim()) ? "New Phone" : Clipboard.GetText().Trim() }; Dbq.Phones.Local.Add(newEml); SelectdPhone = newEml; } catch (Exception ex) { GSReport += $"FAILED. \r\n  {ex.Message}"; ex.Pop(); }
    }

    [ObservableProperty] ICollectionView? emailCvs;

    [ObservableProperty] Phone? selectdPhone; partial void OnSelectdPhoneChanged(Phone? value)
    {
        if (value is null || !_loaded) return;

        var emails = Dbq.PhoneEmailXrefs.Local
            .Where(x => x.PhoneId == value.Id)
            .Join(Dbq.Emails.Local,
                xref => xref.EmailId,
                email => email.Id,
                (xref, email) => email);

        EmailCvs = CollectionViewSource.GetDefaultView(emails.ToList()); //tu: ?? instead of .LoadAsync() / .Local.ToObservableCollection() ?? === PageCvs = CollectionViewSource.GetDefaultView(await Dbq.Phones.ToListAsync());
        //EmailCvs.SortDescriptions.Add(new SortDescription(nameof(Phone.AddedAt), ListSortDirection.Descending));
        //EmailCvs.Filter = obj => obj is not Phone r || r is null ||
        //  string.IsNullOrEmpty(SearchText) ||
        //    r.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
        //    r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

        SelectdEmail = null; // remove white highlight
        Bpr.Tick();
    }
}