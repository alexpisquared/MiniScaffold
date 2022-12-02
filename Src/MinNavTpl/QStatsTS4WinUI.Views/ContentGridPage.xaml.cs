namespace QStatsTS4WinUI.Views;

public sealed partial class ContentGridPage : Page
{
    public ContentGridViewModel ViewModel
    {
        get;
    }

    public ContentGridPage()
    {
        ViewModel = PageHelpers.GetService<ContentGridViewModel>();
        InitializeComponent();
    }
}
