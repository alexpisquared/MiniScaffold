﻿using EF.DbHelper.Lib;

namespace MinNavTpl.VM;
public static class MvvmInitHelper
{
  public static void InitMVVM(IServiceCollection services)
  {
    _ = services.AddSingleton<SrvrStore>();
    _ = services.AddSingleton<DtBsStore>();
    _ = services.AddSingleton<NavigationStore>();
    _ = services.AddSingleton<AllowWriteDBStore>();

    if (!VersionHelper.IsDbg && Environment.GetCommandLineArgs().Length > 4)
      _ = services.AddSingleton<INavSvc, Page00NavSvc>(); //tu: Start Page controller.
    else
      _ = services.AddSingleton<INavSvc, Page01NavSvc>(); //tu: Start Page controller.

    _ = services.AddSingleton<ICompositeNavSvc, CompositeNavSvc>();
    _ = services.AddSingleton<Page00NavSvc>();
    _ = services.AddSingleton<Page01NavSvc>();
    _ = services.AddSingleton<Page02NavSvc>();
    _ = services.AddSingleton<Page03NavSvc>();

    _ = services.AddSingleton(s => new Func<NavBarVM>(() => s.GetRequiredService<NavBarVM>()!));
    _ = services.AddSingleton(s => new Func<Page00VM>(() => s.GetRequiredService<Page00VM>()!));
    _ = services.AddSingleton(s => new Func<Page01VM>(() => s.GetRequiredService<Page01VM>()!));
    _ = services.AddSingleton(s => new Func<Page02VM>(() => s.GetRequiredService<Page02VM>()!));
    _ = services.AddSingleton(s => new Func<Page03VM>(() => s.GetRequiredService<Page03VM>()!));

    _ = services.AddTransient<NavBarVM>();
    _ = services.AddSingleton<MainVM>();
    _ = services.AddTransient<Page00VM>();
    _ = services.AddTransient<Page01VM>();
    _ = services.AddTransient<Page03VM>();
    _ = services.AddTransient<Page02VM>();

    _ = services.AddTransient<ISecForcer, CI.SecurityEnforcement.Mok.SecForcer>();
    _ = services.AddTransient<SqlPermissionsManager>();
    _ = services.AddTransient<BmsPermissionsManager>();

    _ = services.AddTransient<UserSettings>();
  }
}