using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

public sealed partial class Blank1Page : Page
{
    public Blank1ViewModel ViewModel
    {
        get;
    }

    public Blank1Page()
    {
        ViewModel = App.GetService<Blank1ViewModel>();
        InitializeComponent();
    }
}
