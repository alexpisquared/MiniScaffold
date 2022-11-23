using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using DB.QStats.Std.Models;
using GigaHunt.AsLink;
using Microsoft.EntityFrameworkCore;
var _now = DateTime.Today;
var _sw = Stopwatch.StartNew();
int _cur = 0, _j = 0, _ttl, _page = 1;
HashSet<string> _vlds = new(), _bads = new();

await ShowPhoneExtraction();
Console.Write("\a");
Console.ResetColor();

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
        InsertPhoneAgencyXRef(dbx, e.EmailId, _now, p.PhoneNumber, p));

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

  OutlookHelper6.GetUniqueValidBadPhoneNumbersFromLetter(ehist, _cur, _ttl, _sw, _vlds, _bads);

  if (!skipDbInsert)
  {
    //InsertPhoneNumbersIntoDB(dbx, pnlst, ehist.EmailId, ehist.EmailedAt);    if (_cur % _page == 0)      Console.Write($"{_cur,8:N0} / {_ttl:N0}    {((_ttl - _cur) * _sw.Elapsed.TotalMinutes / _cur),5:N1} min left    {dbx.SaveChanges(),5} rowsSaved saved.\n");
  }
}

void InsertPhoneNumbersIntoDB(QstatsRlsContext dbx, List<string> pnLst, string emailId, DateTime emailedAt) { pnLst.ForEach(pn => InsertPhoneNumberIntoDB(dbx, emailId, emailedAt, _now, pn)); }

static void InsertPhoneNumberIntoDB(QstatsRlsContext dbx, string emailId, DateTime emailedAt, DateTime _now, string phnum)
{
  var phone = dbx.Phones.Local.FirstOrDefault(r => r.PhoneNumber == phnum);
  if (phone is not null)
  {
    if (phone.SeenFirst > emailedAt) phone.SeenFirst = emailedAt;
    else
    if (phone.SeenLast < emailedAt) phone.SeenLast = emailedAt;

    //InsertPhoneEmailXRef(dbx, emailId, _now, phnum, phone);
    InsertPhoneAgencyXRef(dbx, emailId, _now, phnum, phone);
  }
  else
  {
    dbx.Phones.Local.Add(new Phone { AddedAt = _now, SeenFirst = emailedAt, SeenLast = emailedAt, PhoneNumber = phnum });
  }
}

static void InsertPhoneEmailXRef(QstatsRlsContext dbx, string emailId, DateTime _now, string phnum, Phone phone)
{
  var email = dbx.Emails.Find(emailId);
  if (email is null)
    Console.Write($"(email is null): {emailId} ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n");
  else
  {
    if (string.IsNullOrEmpty(email.Phone)) email.Phone = phnum;

    if (!dbx.PhoneEmailXrefs.Local.Any(r => r.PhoneId == phone.Id && r.EmailId == email.Id))
    {
      dbx.PhoneEmailXrefs.Local.Add(new PhoneEmailXref { PhoneId = phone.Id, EmailId = email.Id, Note = "", AddedAt = _now });
    }
  }
}

static void InsertPhoneAgencyXRef(QstatsRlsContext dbx, string emailId, DateTime _now, string phnum, Phone phone)
{
  var agencyId = GetCompany(emailId);
  var agency = dbx.Agencies.Find(agencyId);
  if (agency is null)
    Console.Write($"agency is null:  {emailId,48} => {agencyId,-36} ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■\n");
  else
  {
    if (!dbx.PhoneAgencyXrefs.Local.Any(r => r.PhoneId == phone.Id && r.AgencyId == agency.Id))
    {
      dbx.PhoneAgencyXrefs.Local.Add(new PhoneAgencyXref { PhoneId = phone.Id, AgencyId = agency.Id, Note = "", AddedAt = _now });
    }
  }
}

static string GetCompany(string email)
{
  return email.Split("@").LastOrDefault()?.Split(".").FirstOrDefault()?.ToLower() ?? "NoCompanyName";
}