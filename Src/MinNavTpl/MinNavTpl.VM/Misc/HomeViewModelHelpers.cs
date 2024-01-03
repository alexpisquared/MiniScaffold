using EF.DbHelper.Lib;

namespace MinNavTpl.VM.Misc;

internal static class Page01VMHelpers
{
  public static void LoadServerDBsFAF(string srvr, IEnumerable dtBsList, Action<int> setLoadedFlag, string connectionString, ILogger lgr)
  {
    if (string.IsNullOrEmpty(srvr)) return;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
    {
      _ = await LoadSrvrDtBssAsyncTask(srvr, dtBsList, setLoadedFlag, connectionString, lgr);
    });
  }

  public static async Task<bool> LoadSrvrDtBssAsyncTask(string srvr, IEnumerable dtBsList, Action<int> setLoadedFlag, string connectionString, ILogger lgr)
  {
    try
    {
      if (string.IsNullOrEmpty(srvr)) return false;

      using SqlConnection sqlconnection = new(connectionString);

      var rows = await SqlSpRuner.ExecuteReaderAsync(sqlconnection, @$"SELECT name, create_date, state_desc, compatibility_level FROM sys.databases WHERE (database_id > 4) ORDER BY name", null, lgr);

      var list = new List<ADDtBs>();
      rows.ToList().ForEach(r => list.Add(new(r.name, r.name, $"state_desc:{r.state_desc}  compatibility_level:{r.compatibility_level}", r.create_date)));

      ((Collection<ADDtBs>)dtBsList).ClearAddRangeAuto(list);

      setLoadedFlag(list.Count);
      return true;
    }
    catch (Exception ex) { ex.Pop(lgr); return false; }
  }
}