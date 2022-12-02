namespace QStatsTS4WinUI.Views;
public sealed partial class Blank1Page : Page
{
    public Blank1ViewModel ViewModel
    {
        get;
    }

    public Blank1Page()
    {
        ViewModel = PageHelpers.GetService<Blank1ViewModel>(); // = App.GetService <Blank1ViewModel>();
        InitializeComponent();
    }
}
