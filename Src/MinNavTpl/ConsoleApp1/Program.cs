using System;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using DB.QStats.Std.Models;
using Microsoft.EntityFrameworkCore;

var _now = DateTime.Today;
var sw = Stopwatch.StartNew();
var ts = TimeSpan.Zero;
int _cur = 0, _j = 0, _ttl, _page = 10000, _take = _page * 10;

await ShowPhoneExtraction();

async Task ShowPhoneExtraction()
{
  using var dbx = QstatsRlsContext.Create();


  Console.ForegroundColor = ConsoleColor.Cyan;

  var rcvds = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222
  //&& r.EmailId.Contains("glass")
  ).OrderBy(r => r.EmailedAt);
  _ttl = await rcvds.CountAsync();
  sw = Stopwatch.StartNew();
  rcvds.Take(_take).ToList().ForEach(eHist => GetPhoneNumbersToDbIf(dbx, eHist, true));

  //var rows = await dbx.SaveChangesAsync();  Console.ForegroundColor = ConsoleColor.Green;  Console.WriteLine($"   {rows,3} rows saved.     All took {sw.Elapsed.TotalMinutes:N0} min.");
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
    $"in {sw.Elapsed.TotalSeconds:N0}s");

  sw = Stopwatch.StartNew();


  Console.ForegroundColor = ConsoleColor.Cyan;

  if (DateTime.Now != DateTime.Today)
  {
    _ttl = dbx.Phones.Local.Count();
    dbx.Phones.Local.Take(_take).ToList().ForEach(p =>
    {
      dbx.PhoneEmailXrefs.Local.Where(e => e.PhoneId == p.Id).ToList().ForEach(e =>
        InsertPhoneAgencyXRef(dbx, e.EmailId, _now, p.PhoneNumber, p));

      if (++_cur % _page == 0)
        Console.Write($"{_cur,8:N0} / {_ttl:N0}    {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),5:N1} min left    {dbx.SaveChanges(),5} rows saved.\n");
    });
  }
  else
  {
    var rcvds = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222);
    _ttl = await rcvds.CountAsync();
    rcvds.Take(_take).ToList().ForEach(r => GetPhoneNumbersToDbIf(dbx, r, false));
  }

  var rows = await dbx.SaveChangesAsync();

  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine($"   {rows,3} rows saved.     All took {sw.Elapsed.TotalMinutes:N0} min.");
}

void GetPhoneNumbersToDbIf(QstatsRlsContext dbx, Ehist ehist, bool skipDbInsert)
{
  new string[] {
    @"((\+|\+\s|\d{1}\s?|\()(\d\)?\s?[-\.\s\(]??){8,}\d{1}|\d{3}[-\.\s]??\d{3}[-\.\s]??\d{4}|\(\d{3}\)\s*\d{3}[-\.\s]??\d{4}|\d{3}[-\.\s]??\d{4})"
  }.ToList().ForEach(r =>
  {
    var pnlst = GetPhoneNumbersFromLetter(ehist, r);

    if (!skipDbInsert)
      InsertPhoneNumbersIntoDB(dbx, pnlst, ehist.EmailId, ehist.EmailedAt);

    if (!skipDbInsert && _cur % _page == 0)
      Console.Write($"{_cur,8:N0} / {_ttl:N0}    {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),5:N1} min left    {dbx.SaveChanges(),5} rows saved.\n");
  });
}

List<string> GetPhoneNumbersFromLetter(Ehist ehist, string regex)
{
  List<string> rv = new();

  _cur++;

  var match = new Regex(regex).Match(ehist.LetterBody!);
  while (match.Success)
  {
    foreach (var pnraw in match.Value.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
      var pn = pnraw
        .Replace(" ", "")
        .Replace(" ", "")
        .Replace("+", "")
        .Replace("_", "")
        .Replace("-", "")
        .Replace(".", "")
        .Replace("(", "")
        .Replace(")", "");

      Trace.Write($"{ehist.Id,5:N0} {ehist.LetterBody?.Length,8:N0}   {ehist.EmailId,40}   ");

      if (pn.Length < 10)
        Trace.Write($"< 10 ---");
      else if (pn.Length > 11)
        Trace.Write($"> 11 ---");
      else if (pn == pnraw)
      {
        if (
          pn.Length == 10 && (pn.StartsWith("416") || pn.StartsWith("647") || pn.StartsWith("905")) ||
          pn.Length == 11 && (pn.StartsWith("1416") || pn.StartsWith("1647") || pn.StartsWith("1905")))
          ; // Console.Write($"{_cur,8:N0} / {_ttl:N0}  {ehist.EmailId,56}  {pnraw,16} {pn,11}   {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),8:N1} min left       {ehist.EmailedAt:yyyy-MM}  + + + \n");
        else
        {
          var idx = ehist.LetterBody?.IndexOf(pnraw) ?? 0;
          if (idx > 10)
          {
            var d = " :+\n\r";
            if (d.Contains(ehist.LetterBody?.Substring(idx - 1, 1)))
              ; //  Console.Write($"{_cur,8:N0} / {_ttl:N0}  {ehist.EmailId,56}  {pnraw,16} {pn,11}   {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),8:N1} min left       {ehist.EmailedAt:yyyy-MM}     {ehist.LetterBody?.Substring(idx - 6, 6)}  +++++++++++++++++++\n");
            else
              Console.Write($"{_cur,8:N0} / {_ttl:N0}  {ehist.EmailId,56}  {pnraw,16} {pn,11}   {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),8:N1} min left       {ehist.EmailedAt:yyyy-MM}     {ehist.LetterBody?.Substring(idx - 16, 16)}  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ Remove me from DB!!!\n");
          }
          else
          {
            Console.Write($"{_cur,8:N0} / {_ttl:N0}  {ehist.EmailId,56}  {pnraw,16} {pn,11}   {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),8:N1} min left       {ehist.EmailedAt:yyyy-MM}  - - -\n");
          }
        }
      }
      else
      {
        if (pn.Length == 11 && pn[0] == '1')
          pn = pn[1..];

        //Console.Write($"{_cur,8:N0} / {_ttl:N0}  {ehist.EmailId,56}  {pnraw,16} {pn,11}   {((_ttl - _cur) * sw.Elapsed.TotalMinutes / _cur),8:N1} min left       {ehist.EmailedAt:yyyy-MM}  \n");

        Trace.Write($"++++++++");
        rv.Add(pn);
      }

      Trace.WriteLine($"    {pn}");
    }

    match = match.NextMatch();
  }

  return rv;
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

static string GetCompany(string email) => email.Split("@").LastOrDefault()?.Split(".").FirstOrDefault()?.ToLower() ?? "NoCompanyName";
