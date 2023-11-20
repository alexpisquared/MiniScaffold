using System.Diagnostics;
using DB.QStats.Std.Models;
using Microsoft.EntityFrameworkCore;
using StandardLib.Consts;

var _now = DateTime.Today;
var _sw = Stopwatch.StartNew();
int _cur = 0, _ttl;
HashSet<string> _vlds = [], _bads = [];

await RemoveBadPhonesFromDB();
//await ShowPhoneExtraction_Plus_Removal();
Console.Write("\a");
Console.ResetColor();

async Task RemoveBadPhonesFromDB()
{
    using var dbx = QstatsRlsContext.Create();

    Console.ForegroundColor = ConsoleColor.Cyan;

    var badPhNums = new List<Phone>();
    await dbx.Phones.OrderBy(r => r.PhoneNumber).ForEachAsync(p =>
    {
        if (p.PhoneNumber.Contains("_") || !AreaCodeValidator.Any(p.PhoneNumber))
        {
            badPhNums.Add(p);
        }
    });

    _ttl = badPhNums.Count;
    _sw = Stopwatch.StartNew();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"   {_ttl} / {dbx.Phones.Count()}   \n");
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    badPhNums.ToList().ForEach(p => Console.Write($"   {++_cur,5}    {p.PhoneNumber}   {p.SeenLast}   {(p.SeenLast - p.SeenFirst).TotalDays,5:N1}  \n"));

    var rv = await CascadingRemoveFromDbAsync(dbx, badPhNums.Select(r => r.PhoneNumber));

    var rowsSaved = await dbx.SaveChangesAsync();
    Console.ForegroundColor = rowsSaved > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray;
    Console.WriteLine($"       {(rowsSaved > 0 ? "■ ■ ■ ■ ==>" : "··········")}  {rowsSaved,5} rowsSaved saved.     All took {_sw.Elapsed.TotalSeconds,5:N0} sec.");
}

async Task<int> CascadingRemoveFromDbAsync(QstatsRlsContext dbx, IEnumerable<string> bads)
{
    foreach (var bad in bads)
    {
        var phone = await dbx.Phones.FirstOrDefaultAsync(r => r.PhoneNumber == bad);
        if (phone is not null)
        {
            foreach (var phoneEm in await dbx.PhoneEmailXrefs.Where(r => r.PhoneId == phone.Id).ToListAsync())
            {
                _ = dbx.PhoneEmailXrefs.Remove(phoneEm);
            }

            foreach (var phoneAg in await dbx.PhoneAgencyXrefs.Where(r => r.PhoneId == phone.Id).ToListAsync())
            {
                _ = dbx.PhoneAgencyXrefs.Remove(phoneAg);
            }

            _ = dbx.Phones.Remove(phone);
        }
    }

    return 0;
}