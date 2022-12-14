namespace MinNavTpl;
public static class AppStartHelper
{
  public static void InitAppSvcs(IServiceCollection services)
  {
    _ = services.AddSingleton<IConfigurationRoot>(ConfigHelper.AutoInitConfigFromFile());

    _ = services.AddSingleton<ILogger>(sp => SeriLogHelper.InitLoggerFactory(
      folder: FSHelper.GetCreateSafeLogFolderAndFile((sp.GetRequiredService<IConfigurationRoot>()[CfgName.LogFolder] ?? "Logs").Replace("..", $"{Assembly.GetExecutingAssembly().GetName().Name![..6]}.{VersionHelper.Env()}.{Environment.UserName[..3]}..")),
      levels: Settings.Default.LogLevels).CreateLogger<MainNavView>());

    _ = services.AddSingleton<IBpr, Bpr>(); // _ = VersionHelper_.IsDbgAndRBD ? services.AddSingleton<IBpr, Bpr>() : services.AddSingleton<IBpr, BprSilentMock>();
  }
}