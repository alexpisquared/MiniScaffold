using QStatsTS4WinUI.Contracts.Services;

namespace QStatsTS4WinUI.Contracts;

public static class PageHelpers
{
    public static T GetService<T>()
        where T : class
    {
        if ((Microsoft.UI.Xaml.Application.Current as IApp)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"▄▀▄▀▄▀ {typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs. ▄▀▄▀▄▀");
        }

        return service;
    }
}