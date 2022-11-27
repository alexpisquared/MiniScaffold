using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

public sealed partial class BlankPage : Page
{
    public BlankViewModel ViewModel
    {
        get;
    }

    public BlankPage()
    {
        ViewModel = App.GetService<BlankViewModel>();
        InitializeComponent();
    }
}
