using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System;
using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;





using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Markup;

namespace QStatsTS4WinUI.Views;
public interface IApp
{
    IHost Host
    {
        get;
    }
}
public sealed partial class BlankPage : Page
{
    public BlankViewModel ViewModel
    {
        get;
    }

    public BlankPage()
    {
        //ViewModel = App.GetService<BlankViewModel>();
        ViewModel = GetService<BlankViewModel>(); Trace.Write(ViewModel.Hello);

        InitializeComponent();
    }

    public static T GetService<T>()
        where T : class
    {
        IApp app = Microsoft.UI.Xaml.Application.Current as IApp;

        IHost h = app!.Host;

        IServiceProvider s = h.Services;

        var c = s.GetService(typeof(T));
        if (c is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }
}
