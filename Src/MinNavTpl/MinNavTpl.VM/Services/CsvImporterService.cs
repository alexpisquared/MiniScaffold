namespace MinNavTpl.VM.Services;
public class CsvImporterService
{
  int _nullRec = 0, _existDb = 0, _succss = 0;

  public CsvImporterService(QstatsRlsContext dbq, ILogger lgr, DateTimeOffset now) => (Dbq, Lgr, Now) = (dbq, lgr, now);

  public ILogger Lgr { get; }
  public QstatsRlsContext Dbq { get; }
  public DateTimeOffset Now { get; }

  public async Task ImportCsvAsync(Action<string> ReportProgress)
  {
    ReportProgress($"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}");
    var sw = Stopwatch.StartNew();

    const string csvFile = @"C:\temp\CS.Patch.csv";      //if (File.Exists(csvFile))        _ = Process.Start("Explorer.exe", $"/select, \"{csvFile}\"");      else        _ = MessageBox.Show($"Failed to create the CSV file \n\n{csvFile} \n\n", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    var i = 0;
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      MissingFieldFound = null,           // Field at index '3' does not exist. You can ignore missing fields by setting MissingFieldFound to null. 
      IgnoreBlankLines = true,
      TrimOptions = TrimOptions.Trim,
      HasHeaderRecord = false
    };

    var lst = new List<CsvRow>();
    var agncies = new List<string>();

    using (var reader = new StreamReader(csvFile))
    using (var csv = new CsvReader(reader, config))
      // csv.ReadHeader();
      while (await csv.ReadAsync())
      {
        var r1 = await TryAddEmailAsync(csv.GetRecord<CsvRow>());
        if (++i % 1000 == 0)
          ReportProgress($"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  ");

        lst.Add(csv.GetRecord<CsvRow>() ?? throw new ArgumentNullException("@@@@@@@@@@@@@@@@@@@@@@@@@@-"));
      }

    //lst.ForEach(async r =>
    //{
    //  var r1 = await TryAddEmailAsync(r);
    //  if (++i % 1000 == 0)
    //    ReportProgress($"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  ");
    //});

    lst.Select(r => r.Email).ToList().ForEach(r =>
    {
      var co = GetCompany(r);
      if (co is null || agncies.Contains(co))
        return;

      agncies.Add(co);
    });

    ReportProgress($"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms   \n\n");

    agncies.ForEach(co =>
    {
      var r1 = TryAddAgency(co);
      if (++i % 10 == 0)
        ReportProgress($"null:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms  \n");
    });

    ReportProgress($"\nnull:{_nullRec,-5}    exst:{_existDb,-5}    sccs:{_succss,-5}   in {sw.ElapsedMilliseconds,8}ms   ");
  }


  async Task<string> TryAddEmailAsync(CsvRow? record)
  {
    if (record is null) { _nullRec++; return "co is NULL"; }

    var eml = record.Email.Replace("@CI", "@unknwn").Replace("@ci", "@unknwn");

    if (await Dbq.Emails.FindAsync(eml) is not null) { _existDb++; return "Already exists in DB"; }

    await Dbq.Emails.AddAsync(new Email
    {
      Id = eml,
      Fname = record.NameL.Split("|").LastOrDefault(),
      Lname = record.NameL.Split("|").FirstOrDefault(),
      LastAction = record.Last,
      Company = GetCompany(record.Email),
      PermBanReason = "naa",
      NotifyPriority = 7654,
      AddedAt = Now.Date
    });

    _succss++;

    return "";
  }
  string TryAddAgency(string co)
  {
    if (co is null) { _nullRec++; return "co is NULL"; }

    if (Dbq.Agencies.Find(co) is not null) { _existDb++; return "Already exists in DB"; }

    Dbq.Agencies.Add(new Agency
    {
      Id = co,
      Note = "from Pg00",
      AddedAt = Now.Date
    });

    _succss++;

    return "";
  }
  public static string GetCompany(string email) => email.Split("@").LastOrDefault()?.Split(".").FirstOrDefault()?.ToLower() ?? "NoCompanyName";

  public class CsvRow
  {
    [CsvHelper.Configuration.Attributes.Index(0)] public string NameL { get; set; } = "";
    [CsvHelper.Configuration.Attributes.Index(1)] public string Email { get; set; } = "";
    [CsvHelper.Configuration.Attributes.Index(2)] public DateTime? Last { get; set; }
  }
}
