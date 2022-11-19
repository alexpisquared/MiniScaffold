using System.Diagnostics;
using System.Text.RegularExpressions;
using DB.QStats.Std.Models;
using Microsoft.EntityFrameworkCore;

//List<string> dbx.Phones.Local = new List<string>();

var _now = DateTime.Now;
var max = 1000000;

Trace.WriteLine("The Start...");
await UseDB();

async Task UseDB()
{
  using var dbx = QstatsRlsContext.Create();
  await dbx.PhoneAgencyXrefs.LoadAsync();
  await dbx.PhoneEmailXrefs.LoadAsync();
  await dbx.Phones.LoadAsync();

  var q = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222).Take(max);
  q.ToList().ForEach(r => FindPhoneNumbers(dbx, r));

  var rows = await dbx.SaveChangesAsync();

  Console.WriteLine($"{dbx.Phones.Local.Count,9:N0} / {q.Count():N0} = {100.0 * dbx.Phones.Local.Count / max:N0} %   {rows} rows savedc.");
}

void FindPhoneNumbers(QstatsRlsContext dbx, Ehist x)
{
  new string[] {
    @"((\+|\+\s|\d{1}\s?|\()(\d\)?\s?[-\.\s\(]??){8,}\d{1}|\d{3}[-\.\s]??\d{3}[-\.\s]??\d{4}|\(\d{3}\)\s*\d{3}[-\.\s]??\d{4}|\d{3}[-\.\s]??\d{4})"
  }.ToList().ForEach(r =>
  {
    var pnlst = GetPhoneNumbersFromLetter(x, r);
    InsertPhoneNumbersIntoDB(dbx, pnlst, x.EmailedAt);
  });
}

List<string> GetPhoneNumbersFromLetter(Ehist ehist, string regex)
{
  List<string> rv = new();
  var rePhone = new Regex(regex);
  var m = rePhone.Match(ehist.LetterBody!);
  while (m.Success)
  {
    foreach (var pnraw in m.Value.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
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
      else
      {
        if (pn.Length == 11 && pn[0] == '1')
          pn = pn[1..];

        Console.Write($"{ehist.Id,5:N0} {ehist.LetterBody?.Length,8:N0}   {ehist.EmailId,88}{pnraw,22}{pn,18}    {ehist.EmailedAt:yyyy-MM-dd}\n");
        Trace.Write($"++++++++");
        rv.Add(pn);
      }

      Trace.WriteLine($"    {pn}");
    }

    m = m.NextMatch();
  }

  return rv;
}

void InsertPhoneNumbersIntoDB(QstatsRlsContext dbx, List<string> pnLst, DateTime emailedAt)
{
  pnLst.ForEach(pn =>
    InsertPhoneNumberIntoDB(dbx, emailedAt, _now, pn));
}

static void InsertPhoneNumberIntoDB(QstatsRlsContext dbx, DateTime emailedAt, DateTime _now, string pn)
{
  var exstg = dbx.Phones.Local.FirstOrDefault(r => r.PhoneNumber == pn);
  if (exstg is not null)
  {
    if (exstg.SeenFirst > emailedAt) exstg.SeenFirst = emailedAt;
    else
    if (exstg.SeenLast < emailedAt) exstg.SeenLast = emailedAt;
  }
  else
  {
    dbx.Phones.Local.Add(new Phone { AddedAt = _now, SeenFirst = emailedAt, SeenLast = emailedAt, PhoneNumber = pn });
  }
}