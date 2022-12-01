namespace QStatsTS4WinUI.Views;

internal static class PageHelpers
{

    public static T GetService<T>()
        where T : class
    {
        var app = Microsoft.UI.Xaml.Application.Current as IApp ?? throw new ArgumentNullException("▄▀▄▀▄▀");
        var h = app.Host;
        var s = h.Services;
        var c = s.GetService(typeof(T));
        if (c is not T service)
        {
            throw new ArgumentException($"▄▀▄▀▄▀ {typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs. ▄▀▄▀▄▀");
        }

        return service;
    }
}