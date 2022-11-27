using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
