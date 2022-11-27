using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

public sealed partial class ContentGridPage : Page
{
    public ContentGridViewModel ViewModel
    {
        get;
    }

    public ContentGridPage()
    {
        ViewModel = App.GetService<ContentGridViewModel>();
        InitializeComponent();
    }
}
