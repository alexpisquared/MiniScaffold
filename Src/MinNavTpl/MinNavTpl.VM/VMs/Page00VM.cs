namespace MinNavTpl.VM.VMs;
public partial class Page00VM : BaseDbVM
{
  readonly DateTimeOffset _now = DateTimeOffset.Now;
  public Page00VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, dbx, win, allowWriteDBStore, usrStgns, 8110) => _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      await Task.Delay(20);
      var sw = Stopwatch.StartNew();
      //await new WpfUserControlLib.Services.ClickOnceUpdater(Lgr).CopyAndLaunch(ReportProgress);
      //await ImportCsv();
      Lgr.Log(LogLevel.Trace, $"DB:  in {sw.ElapsedMilliseconds,8}ms  at SQL:{UserSetgs.PrefSrvrName} ▀▄▀▄▀▄▀▄▀");
      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  void ReportProgress(string msg) => ReportMessage = msg;

  [ObservableProperty] string reportMessage = ":0";

  [RelayCommand]
  async Task SaveDb()
  {
    try
    {
      await Bpr.TickAsync();
      Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}");
      var sw = Stopwatch.StartNew();
      var rv = await Dbx.TrySaveReportAsync();
      Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  at {UserSetgs.PrefDtBsName} \n\t{rv}");

      await Bpr.TickAsync();
    }
    catch (Exception ex) { IsBusy = false; WriteLine(ex.Message); ex.Pop(Lgr); }
    finally { IsBusy = _saving = false; Bpr.Tick(); }
  }


  [RelayCommand]
  async Task ImportCsv()
  {
    try
    {
      await Bpr.TickAsync();
      Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}");
      var sw = Stopwatch.StartNew();

      const string csvFile = @"C:\temp\CS.Patch.csv";      //if (File.Exists(csvFile))        _ = Process.Start("Explorer.exe", $"/select, \"{csvFile}\"");      else        _ = MessageBox.Show($"Failed to create the CSV file \n\n{csvFile} \n\n", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      int i = 0;
      var config = new CsvConfiguration(CultureInfo.InvariantCulture)
      {
        MissingFieldFound = null,           // Field at index '3' does not exist. You can ignore missing fields by setting MissingFieldFound to null. 
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        HasHeaderRecord = false
      };

      var lst = new List<Foo>();
      var agncies = new List<string>();

      using (var reader = new StreamReader(csvFile))
      using (var csv = new CsvReader(reader, config))
      {
        // csv.ReadHeader();
        while (csv.Read())
        {
          var r1 = await tryAdd(csv.GetRecord<Foo>());
          if (++i % 1000 == 0)
            Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  ");

          lst.Add(csv.GetRecord<Foo>() ?? throw new ArgumentNullException("@@@@@@@@@@@@@@@@@@@@@@@@@@"));
        }
      }

      //lst.ForEach(async r =>
      //{
      //  var r1 = await tryAdd(r);
      //  if (++i % 1000 == 0)
      //    Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  ");
      //});

      lst.Select(r => r.Email).ToList().ForEach(r =>
      {
        var co = GetCompany(r);
        if (co is null || agncies.Contains(co))
          return;

        agncies.Add(co);
      });

      Lgr.Log(LogLevel.Trace, ReportMessage = $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  at {UserSetgs.PrefDtBsName} \n\n");

      agncies.ForEach(co =>
      {
        var r1 = tryAddAgency(co);
        if (++i % 10 == 0)
          Lgr.Log(LogLevel.Trace, ReportMessage += $"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  \n");
      });

      Lgr.Log(LogLevel.Trace, ReportMessage += $"\nnull:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  at {UserSetgs.PrefDtBsName} ");

      await Bpr.TickAsync();
    }
    catch (Exception ex) { IsBusy = false; WriteLine(ex.Message); ex.Pop(Lgr); }
    finally { IsBusy = _saving = false; Bpr.Tick(); }
  }

  int _nullRec = 0, _existDb = 0, _succss = 0;
  async Task<string> tryAdd(Foo? record)
  {
    if (record is null) { _nullRec++; return "co is NULL"; }

    var eml = record.Email.Replace("@CI", "@unknwn").Replace("@ci", "@unknwn");

    if ((await Dbx.Emails.FindAsync(eml)) is not null) { _existDb++; return "Already exists in DB"; }

    await Dbx.Emails.AddAsync(new Email
    {
      Id = eml,
      Fname = record.NameL.Split("|").LastOrDefault(),
      Lname = record.NameL.Split("|").FirstOrDefault(),
      LastAction = record.Last,
      Company = GetCompany(record.Email),
      PermBanReason = "naa",
      NotifyPriority = 999,
      AddedAt = _now.Date
    });

    _succss++;

    return "";
  }

  static string? GetCompany(string email) => email.Split("@").LastOrDefault()?.Split(".").FirstOrDefault()?.ToLower();

  string tryAddAgency(string co)
  {
    if (co is null) { _nullRec++; return "co is NULL"; }

    if ((Dbx.Agencies.Find(co)) is not null) { _existDb++; return "Already exists in DB"; }

    Dbx.Agencies.Add(new Agency
    {
      Id = co,
      Note = "from Pg00",
      AddedAt = _now.Date
    });

    _succss++;

    return "";
  }

  public class Foo
  {
    [CsvHelper.Configuration.Attributes.Index(0)] public string NameL { get; set; } = "";
    [CsvHelper.Configuration.Attributes.Index(1)] public string Email { get; set; } = "";
    [CsvHelper.Configuration.Attributes.Index(2)] public DateTime? Last { get; set; }
  }

}