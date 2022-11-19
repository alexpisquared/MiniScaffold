using System.Diagnostics;
using System.Text.RegularExpressions;
using DB.QStats.Std.Models;

List<string> pns = new List<string>();

int max = 100000;

Trace.WriteLine("The Start...");
UseDB();

void UseDB()
{
  using var dbx = QstatsRlsContext.Create();
  var q = dbx.Ehists.Where(r => r.RecivedOrSent != "S" && r.LetterBody != null && r.LetterBody.Length > 222).Take(max);
  q.ToList().ForEach(FindPhoneNumbers);

  Console.WriteLine($"{pns.Count,9:N0} / {q.Count():N0} = {(100.0 * pns.Count / max):N0} %");
}

void FindPhoneNumbers(Ehist x)
{
  new string[] {
    @"((\+|\+\s|\d{1}\s?|\()(\d\)?\s?[-\.\s\(]??){8,}\d{1}|\d{3}[-\.\s]??\d{3}[-\.\s]??\d{4}|\(\d{3}\)\s*\d{3}[-\.\s]??\d{4}|\d{3}[-\.\s]??\d{4})"
  }.ToList().ForEach(r => FindPhoneNumbers2(x, r));
}

void FindPhoneNumbers2(Ehist x, string regex)
{
  var rePhone = new Regex(regex);
  var m = rePhone.Match(x.LetterBody!);
  while (m.Success)
  {
    foreach (var item in m.Value.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
      var pn = item
        .Replace(" ", "")
        .Replace(" ", "")
        .Replace("+", "")
        .Replace("_", "")
        .Replace("-", "")
        .Replace(".", "")
        .Replace("(", "")
        .Replace(")", "");

      Trace.Write($"{x.Id,5:N0} {x.LetterBody?.Length,8:N0}   {x.EmailId,40}   ");

      if (pn.Length < 10)
        Trace.Write($"< 10 ---");
      else if (pn.Length > 11)
        Trace.Write($"> 11 ---");
      else if (pns.Contains(pn))
        Trace.Write($"@@@@@@@@");
      else
      {
        Console.Write($"{x.Id,5:N0} {x.LetterBody?.Length,8:N0}   {x.EmailId,88}{pn,18}    {x.EmailedAt:yyyy-MM-dd}\n");
        Trace.Write($"++++++++");
        pns.Add(pn);
      }

      Trace.WriteLine($"    {pn}");
    }

    m = m.NextMatch();
  }
}