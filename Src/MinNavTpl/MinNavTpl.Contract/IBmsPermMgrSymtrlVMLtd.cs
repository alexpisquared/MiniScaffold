using System.Runtime.CompilerServices;
using DB.Inventory.Models;
using StandardContracts.Lib;

namespace MinimalNavTemplate.Contract;

public interface IPage02VMLtd
{
  IBpr Bpr { get; }
  void DoDgSelChngdPerm(Permission lastSelectPerm);
  InventoryContext DbxInv { get; }
  Task<int> SyncToSqlAdd(PermissionAssignment pa);
  Task<int> SyncToSqlRmv(PermissionAssignment pa);
  Microsoft.Extensions.Logging.ILogger Logger { get; }
  Task LoadEF(bool isAsync = true);
  Task<string> SaveLogReportOrThrow(Microsoft.EntityFrameworkCore.DbContext dbContext, string note = "", [CallerMemberName] string? cmn = "");
  void DoDgSelChngdUser(User lastSelectUser);
  void CheckDb();

  bool IsBusy { get; set; }
}
