using DB.Inventory.Models;
using EF.DbHelper.Lib;
using MinimalNavTemplate.View;

namespace MinimalNavTemplate;
public static class AppStartHelper
{
  public static void InitAppSvcs(IServiceCollection services)
  {
    _ = services.AddSingleton<IConfigurationRoot>(ConfigHelper.AutoInitConfigHardcoded());

    _ = services.AddSingleton<ILogger>(sp => SeriLogHelper.InitLoggerFactory(
      folder: FSHelper.GetCreateSafeLogFolderAndFile((sp.GetRequiredService<IConfigurationRoot>()[CfgName.LogFolder] ?? "Logs").Replace("..", $"{Assembly.GetExecutingAssembly().GetName().Name![..6]}.{VersionHelper.Env()}.{Environment.UserName[..3]}..")),
      levels: Settings.Default.LogLevels).CreateLogger<MainNavView>());

    _ = services.AddSingleton<IBpr, Bpr>(); // _ = VersionHelper_.IsDbgAndRBD ? services.AddSingleton<IBpr, Bpr>() : services.AddSingleton<IBpr, BprSilentMock>();

    _ = services.AddTransient(sp => new InventoryContext(DbxExt.CalcConStr<InventoryContext>(sp, DevOps.IsDevMachineH ? @".\SqlExpress" : @"mtDEVsqlDB", CfgName.SqlVerIpm)));
  }
}
