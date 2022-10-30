using System.Runtime.CompilerServices;
using DB.QStats.Std.Models;
using StandardContractsLib;

namespace MinNavTpl.Contract;

public interface IPage02VMLtd
{
  //void DoDgSelChngdPerm(Permission lastSelectPerm);
  //Task<int> SyncToSqlAdd(PermissionAssignment pa);
  //Task<int> SyncToSqlRmv(PermissionAssignment pa);
  //void DoDgSelChngdUser(User lastSelectUser);
  IBpr Bpr { get; }
  QStatsRlsContext DbxInv { get; }
  Microsoft.Extensions.Logging.ILogger Logger { get; }
  Task LoadEF(bool isAsync = true);
  Task<string> SaveLogReportOrThrow(Microsoft.EntityFrameworkCore.DbContext dbContext, string note = "", [CallerMemberName] string? cmn = "");
  void CheckDb();

  bool IsBusy { get; set; }
}
