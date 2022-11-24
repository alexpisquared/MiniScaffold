using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using DB.QStats.Std.Models;
using Emailing.NET6;
using GigaHunt.AsLink;
using Microsoft.EntityFrameworkCore;
using StandardLib.Consts;

var _now = DateTime.Today;
var _sw = Stopwatch.StartNew();
int _cur = 0, _j = 0, _ttl, _page = 1;
HashSet<string> _vlds = new(), _bads = new();

await ShowBadPhonesFromDB();
//await ShowPhoneExtraction();
Console.Write("\a");
Console.ResetColor();

async Task ShowBadPhonesFromDB()
{
  using var dbx = QstatsRlsContext.Create();

  Console.ForegroundColor = ConsoleColor.Cyan;

  var badPhNums = new List<Phone>();
  await dbx.Phones.OrderBy(r => r.PhoneNumber).ForEachAsync(p =>
  {
    if (p.PhoneNumber.Contains("_") || !AreaCodeValidator.Any(p.PhoneNumber))
      badPhNums.Add(p);
  });

  _ttl = badPhNums.Count;
  _sw = Stopwatch.StartNew();

  Console.ForegroundColor = ConsoleColor.Cyan;
  Console.Write($"   {_ttl} / {dbx.Phones.Count()}   \n");
  Console.ForegroundColor = ConsoleColor.DarkCyan;
  badPhNums.ToList().ForEach(p => Console.Write($"   {++_cur,5}    {p.PhoneNumber}   {p.SeenLast}   {(p.SeenLast - p.SeenFirst).TotalDays,5:N1}  \n"));

  var rv = await RemoveBadsFromDbAsync(dbx, badPhNums.Select(r => r.PhoneNumber));

  var rowsSaved = await dbx.SaveChangesAsync();
  Console.ForegroundColor = rowsSaved > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray;
  Console.WriteLine($"       {(rowsSaved > 0 ? "■ ■ ■ ■ ==>" : "··········")}  {rowsSaved,5} rowsSaved saved.     All took {_sw.Elapsed.TotalSeconds,5:N0} sec.");
}
async Task ShowPhoneExtraction()
{
  using var dbx = QstatsRlsContext.Create();

  Console.ForegroundColor = ConsoleColor.Cyan;

  var rcvds = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222 && r.EmailedAt > new DateTime(2022, 11, 11)  //&& r.EmailId.Contains("glass")
    ).OrderBy(r => r.EmailedAt);
  _ttl = await rcvds.CountAsync();
  _sw = Stopwatch.StartNew();

  rcvds.ToList().ForEach(eHist => GetPhoneNumbersToDbIf(dbx, eHist, true)); ///////////////////////////////////////////////////

  var bads = _bads.Except(_vlds);
  Console.ForegroundColor = ConsoleColor.DarkCyan; Console.Write($" ttl: {_ttl}   v/b: {_vlds.Count,5:N0} / {_bads.Count,-5:N0} - {_vlds.Intersect(_bads).Count(),8:N0} = {bads.Count(),-8:N0}    {_sw.Elapsed.TotalSeconds,5:N0} sec took \n");

  var rv = await RemoveBadsFromDbAsync(dbx, bads);

  var rowsSaved = await dbx.SaveChangesAsync();
  Console.ForegroundColor = rowsSaved > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray; Console.WriteLine($"       {(rowsSaved > 0 ? "■ ■ ■ ■ ==>" : "··········")}  {rowsSaved,5} rowsSaved saved.     All took {_sw.Elapsed.TotalSeconds,5:N0} sec.");

  Console.ForegroundColor = ConsoleColor.DarkGray;
}

async Task<int> RemoveBadsFromDbAsync(QstatsRlsContext dbx, IEnumerable<string> bads)
{
  foreach (var bad in bads)
  {
    var phone = await dbx.Phones.FirstOrDefaultAsync(r => r.PhoneNumber == bad);
    if (phone is not null)
    {
      foreach (var phoneEm in await dbx.PhoneEmailXrefs.Where(r => r.PhoneId == phone.Id).ToListAsync())
      {
        dbx.PhoneEmailXrefs.Remove(phoneEm);
      }

      foreach (var phoneAg in await dbx.PhoneAgencyXrefs.Where(r => r.PhoneId == phone.Id).ToListAsync())
      {
        dbx.PhoneAgencyXrefs.Remove(phoneAg);
      }

      dbx.Phones.Remove(phone);
    }
  }
  return 0;
}

HashSet<string> RemoveDupes(List<string> bad)
{
  var set = new HashSet<string>();

  foreach (var item in bad)
  {
    _ = set.Add(item);
  }

  return set;
}

async Task LoadAsyncFindAdd()
{
  using var dbx = QstatsRlsContext.Create();
  await dbx.PhoneAgencyXrefs.LoadAsync();
  await dbx.PhoneEmailXrefs.LoadAsync();
  await dbx.Phones.LoadAsync();

  Console.WriteLine(
    $"Phones {dbx.Phones.Local.Count:N0}     " +
    $"Emails {dbx.PhoneEmailXrefs.Local.Count:N0}     " +
    $"Compns {dbx.PhoneAgencyXrefs.Local.Count:N0}     " +
    $"in {_sw?.Elapsed.TotalSeconds:N0}s");

  _sw = Stopwatch.StartNew();

  Console.ForegroundColor = ConsoleColor.Cyan;

  if (DateTime.Now != DateTime.Today)
  {
    _ttl = dbx.Phones.Local.Count;
    dbx.Phones.Local.ToList().ForEach(p =>
    {
      dbx.PhoneEmailXrefs.Local.Where(e => e.PhoneId == p.Id).ToList().ForEach(e =>
        QStatsDbHelper.InsertPhoneAgencyXRef(dbx, e.EmailId, _now, p.PhoneNumber, p));

      if (++_cur % _page == 0)
        Console.Write($"{_cur,8:N0} / {_ttl:N0}    {(_ttl - _cur) * _sw.Elapsed.TotalMinutes / _cur,5:N1} min left    {dbx.SaveChanges(),5} rowsSaved saved.\n");
    });
  }
  else
  {
    var rcvds = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222);
    _ttl = await rcvds.CountAsync();
    rcvds.ToList().ForEach(r => GetPhoneNumbersToDbIf(dbx, r, false));
  }

  var rows = await dbx.SaveChangesAsync();

  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine($"   {rows,3} rowsSaved saved.     All took {_sw.Elapsed.TotalMinutes:N0} min.");
}

void GetPhoneNumbersToDbIf(QstatsRlsContext dbx, Ehist ehist, bool skipDbInsert)
{
  _cur++;

  RegexHelper.ShowUniqueValidBadPhoneNumbersFromLetter(ehist, _cur, _ttl, _sw, _vlds, _bads);

  if (!skipDbInsert)
  {
    //InsertPhoneNumbersIntoDB(dbx, pnlst, ehist.EmailId, ehist.EmailedAt, _now);    if (_cur % _page == 0)      Console.Write($"{_cur,8:N0} / {_ttl:N0}    {((_ttl - _cur) * _sw.Elapsed.TotalMinutes / _cur),5:N1} min left    {dbx.SaveChanges(),5} rowsSaved saved.\n");
  }
}