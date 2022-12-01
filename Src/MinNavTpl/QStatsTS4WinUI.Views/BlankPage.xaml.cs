namespace QStatsTS4WinUI.Views;
public sealed partial class BlankPage : Page
{
    public BlankViewModel ViewModel
    {
        get;
    }

    public BlankPage()
    {
        ViewModel = PageHelpers.GetService<BlankViewModel>(); // = App.GetService<BlankViewModel>();
        Trace.Write(ViewModel.Hello);

        InitializeComponent();
    }
}
