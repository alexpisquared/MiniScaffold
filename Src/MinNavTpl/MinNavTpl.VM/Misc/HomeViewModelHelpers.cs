namespace MinimalNavTemplate.VM.Misc;

internal static class Page01VMHelpers
{
  public static void LoadServerDBsFAF(string srvr, IEnumerable dtBsList, Action<int> setLoadedFlag, string connectionString, ILogger lgr)
  {
    if (string.IsNullOrEmpty(srvr)) return;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
    {
      _ = await LoadSrvrDtBssTask(srvr, dtBsList, setLoadedFlag, connectionString, lgr);
    });
  }
  public static void LoadDtBsRolesFAF(string dtbs, IEnumerable roleList, SqlPermissionsManager spm, string connectionString, ILogger lgr)
  {
    if (string.IsNullOrEmpty(dtbs)) return;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
    {
      if (string.IsNullOrEmpty(dtbs)) return;
      var sqlconnection = await LoadDtBsRolesTask(roleList, spm, connectionString, lgr);
    });
  }
  public static void MarkRoleUsersFAF(string role, IEnumerable? allUsers, SqlPermissionsManager spm, string connectionString)
  {
    if (string.IsNullOrEmpty(role)) return;

    ArgumentNullException.ThrowIfNull(allUsers, nameof(allUsers));

    _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
    {
      using SqlConnection sqlconnection = new(connectionString);

      var rows = await spm.GetUsersForRole(sqlconnection, role);

      foreach (ADUser u in allUsers)
      {
        //if (rows.Contains(u.DomainUsername, new IgnoreCaseComparer()))          WriteLine($"**** {u.DomainUsername,-32} --- {rows.Contains(u.DomainUsername, new IgnoreCaseComparer())}");
        u.IsMemberOfGivenRole = rows.Contains(u.DomainUsername, new IgnoreCaseComparer());
      }
    });
  }

  public static async Task<bool> LoadSrvrDtBssTask(string srvr, IEnumerable dtBsList, Action<int> setLoadedFlag, string connectionString, ILogger lgr)
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
  public static async Task<bool> LoadDtBsRolesTask(IEnumerable roleList, SqlPermissionsManager spm, string connectionString, ILogger lgr)
  {
    try
    {
      SqlConnection sqlconnection = new(connectionString);

      var rows = await spm.GetDbRoles(sqlconnection);

      var list = new List<ADRole>();
      rows.ToList().ForEach(r => list.Add(new(r, r, $"*** {r} ***")));

      ((Collection<ADRole>)roleList).ClearAddRangeAuto(list);
      return true;
    }
    catch (Exception ex) { ex.Pop(lgr); return false; }
  }
}