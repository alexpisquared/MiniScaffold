namespace MinNavTpl;
public static class AppStartHelper
{
    public static void InitAppSvcs(IServiceCollection services)
    {
        _ = services.AddSingleton<IConfigurationRoot>(ConfigHelper.AutoInitConfigFromFile());

        _ = services.AddSingleton<ILogger>(sp => SeriLogHelper.CreateFallbackLogger<MainNavView>(Settings.Default.LogLevels));

        _ = services.AddSingleton<IBpr, Bpr>(); // _ = VersionHelper_.IsDbgAndRBD ? services.AddSingleton<IBpr, Bpr>() : services.AddSingleton<IBpr, BprSilentMock>();

        _ = services.AddSingleton<ISpeechSynth>(sp => SpeechSynth.Factory(
            speechKey: sp.GetRequiredService<IConfigurationRoot>()["AppSecrets:MagicSpeech"] ?? throw new ArgumentNullException("\"AppSecrets:MagicSpeech\" is not available."),
            sp.GetRequiredService<ILogger>(),
            voice: "en-GB-SoniaNeural"));
    }
}