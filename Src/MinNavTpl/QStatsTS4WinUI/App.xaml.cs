namespace QStatsTS4WinUI;
// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application, IApp
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            _ = services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            _ = services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            _ = services.AddSingleton<IAppNotificationService, AppNotificationService>();
            _ = services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            _ = services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            _ = services.AddTransient<INavigationViewService, NavigationViewService>();

            _ = services.AddSingleton<IActivationService, ActivationService>();
            _ = services.AddSingleton<IPageService, PageService>();
            _ = services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            _ = services.AddSingleton<ISampleDataService, SampleDataService>();
            _ = services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            _ = services.AddTransient<SettingsViewModel>();
            _ = services.AddTransient<SettingsPage>();
            _ = services.AddTransient<DataGrid3ViewModel>();
            _ = services.AddTransient<DataGrid3Page>();
            _ = services.AddTransient<DataGrid2ViewModel>();
            _ = services.AddTransient<DataGrid2Page>();
            _ = services.AddTransient<DataGrid1ViewModel>();
            _ = services.AddTransient<DataGrid1Page>();
            _ = services.AddTransient<ContentGridDetailViewModel>();
            _ = services.AddTransient<ContentGridDetailPage>();
            _ = services.AddTransient<ContentGridViewModel>();
            _ = services.AddTransient<ContentGridPage>();
            _ = services.AddTransient<ListDetailsViewModel>();
            _ = services.AddTransient<ListDetailsPage>();
            _ = services.AddTransient<DataGridViewModel>();
            _ = services.AddTransient<DataGridPage>();
            _ = services.AddTransient<Blank1ViewModel>();
            _ = services.AddTransient<Blank1Page>();
            _ = services.AddTransient<BlankViewModel>();
            _ = services.AddTransient<BlankPage>();
            _ = services.AddTransient<MainViewModel>();
            _ = services.AddTransient<MainPage>();
            _ = services.AddTransient<ShellPage>();
            _ = services.AddTransient<ShellViewModel>();

            // Configuration
            _ = services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            // ...Also
            var cfg = new ConfigurationBuilder().AddUserSecrets<WhatIsThatForType>().Build();
            _ = services.AddSingleton<IConfigurationRoot>(cfg);

            _ = services.AddTransient(sp =>
            {
                var cfg = sp.GetRequiredService<IConfigurationRoot>();
                return new QstatsRlsContext(cfg?["SqlConStr"] ?? throw new ArgumentNullException("llakfjasldf"));
            });
        }).
        Build();
        AppHelpers.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }
    class WhatIsThatForType
    {
        public string MyProperty { get; } = "<Default Value of Nothing Special>";
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        AppHelpers.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await AppHelpers.GetService<IActivationService>().ActivateAsync(args);
    }
}