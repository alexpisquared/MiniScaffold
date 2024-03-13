using Db.MinFinInv.PowerTools.Models;

namespace MinNavTpl.VM;
public static class MvvmInitHelper
{
    public static void InitMVVM(IServiceCollection services)
    {
        _ = services.AddSingleton<NavigationStore>();
        _ = services.AddSingleton<SrvrNameStore>();
        _ = services.AddSingleton<DtBsNameStore>();
        _ = services.AddSingleton<GSReportStore>();
        _ = services.AddSingleton<EmailOfIStore>();
        _ = services.AddSingleton<LetDbChgStore>();
        _ = services.AddSingleton<IsBusy__Store>();

        //tu: Start Page startup Page controller.
        if (VersionHelper.IsDbg) /*                               */ _ = services.AddSingleton<INavSvc, Page07NavSvc>(); // Phone
        else if (Environment.GetCommandLineArgs().Contains("Email")) _ = services.AddSingleton<INavSvc, Page01NavSvc>(); // Agent
        else if (Environment.GetCommandLineArgs().Contains("Broad")) _ = services.AddSingleton<INavSvc, Page02NavSvc>(); // Broad   
        else if (Environment.GetCommandLineArgs().Contains("Ou2Db")) _ = services.AddSingleton<INavSvc, Page03NavSvc>(); // Ou2Db
        else if (Environment.GetCommandLineArgs().Contains("Compa")) _ = services.AddSingleton<INavSvc, Page05NavSvc>(); // Compa
        else if (Environment.GetCommandLineArgs().Contains("Phone")) _ = services.AddSingleton<INavSvc, Page07NavSvc>(); // Phone
        else if (Environment.GetCommandLineArgs().Contains("Leads")) _ = services.AddSingleton<INavSvc, Page04NavSvc>(); // Leads
        else if (Environment.GetCommandLineArgs().Length > 4) /*  */ _ = services.AddSingleton<INavSvc, Page04NavSvc>(); // Leads
        else if (Clipboard.ContainsText() && Clipboard.GetText().Trim().Length < 48 && RegexHelper.IsEmail(Clipboard.GetText().Trim())) _ = services.AddSingleton<INavSvc, Page02NavSvc>(); // Broad
        else /*                                                   */ _ = services.AddSingleton<INavSvc, Page01NavSvc>(); // Agent

        _ = services.AddSingleton<ICompositeNavSvc, CompositeNavSvc>();
        _ = services.AddSingleton<Page00NavSvc>();
        _ = services.AddSingleton<Page01NavSvc>();
        _ = services.AddSingleton<Page02NavSvc>();
        _ = services.AddSingleton<Page03NavSvc>();
        _ = services.AddSingleton<Page04NavSvc>();
        _ = services.AddSingleton<Page05NavSvc>();
        _ = services.AddSingleton<Page06NavSvc>();
        _ = services.AddSingleton<Page07NavSvc>();
        _ = services.AddSingleton<EmailDetailNavSvc>();

        _ = services.AddSingleton(s => new Func<NavBarVM>(() => s.GetRequiredService<NavBarVM>()!));
        _ = services.AddSingleton(s => new Func<Page00VM>(() => s.GetRequiredService<Page00VM>()!));
        _ = services.AddSingleton(s => new Func<Page01VM>(() => s.GetRequiredService<Page01VM>()!));
        _ = services.AddSingleton(s => new Func<Page02VM>(() => s.GetRequiredService<Page02VM>()!));
        _ = services.AddSingleton(s => new Func<Page03VM>(() => s.GetRequiredService<Page03VM>()!));
        _ = services.AddSingleton(s => new Func<Page04VM>(() => s.GetRequiredService<Page04VM>()!));
        _ = services.AddSingleton(s => new Func<Page05VM>(() => s.GetRequiredService<Page05VM>()!));
        _ = services.AddSingleton(s => new Func<Page06VM>(() => s.GetRequiredService<Page06VM>()!));
        _ = services.AddSingleton(s => new Func<Page07VM>(() => s.GetRequiredService<Page07VM>()!));
        _ = services.AddSingleton(s => new Func<EmailDetailVM>(() => s.GetRequiredService<EmailDetailVM>()!));

        _ = services.AddTransient<NavBarVM>();
        _ = services.AddSingleton<MainVM>();
        _ = services.AddTransient<Page00VM>();
        _ = services.AddTransient<Page01VM>();
        _ = services.AddTransient<Page02VM>();
        _ = services.AddTransient<Page03VM>();
        _ = services.AddTransient<Page04VM>();
        _ = services.AddTransient<Page05VM>();
        _ = services.AddTransient<Page06VM>();
        _ = services.AddTransient<Page07VM>();
        _ = services.AddTransient<EmailDetailVM>();

        _ = services.AddTransient<ISecurityForcer, SecurityForcer>();

        _ = services.AddSingleton<UserSettings>();

        _ = services.AddTransient(sp => new QstatsRlsContext(CalcConStr<QstatsRlsContext>(sp, CfgName.SqlVerIpm)));
        _ = services.AddTransient(sp => new MinFinInvDbContext());
    }

    public static string CalcConStr<T>(IServiceProvider sp, string sqlver)
    {
        var stg = sp.GetRequiredService<UserSettings>();
        var cfg = sp.GetRequiredService<IConfigurationRoot>();
        return string.Format(cfg[sqlver]!, stg.SrvrName, stg.DtBsName, "IpmDevDbgUser", "IpmDevDbgUser");
    }
}