namespace MinNavTpl.View;
public partial class MainNavView : WpfUserControlLib.Base.WindowBase
{
    public MainNavView(ILogger logger) : this((ILogger<Window>)logger) { }
    public MainNavView(ILogger<Window> logger) : base(logger)
    {
        InitializeComponent();

        //tu: ..if needed!!! MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight; // MaximumWindowTrackHeight; // 2021-11: is it for not overlapping the taskbar?

        //themeSelector1.ThemeApplier = ApplyTheme; //dnf theming 1/2

        btnExit.IsCancel = true; // for enterpise only: = VersionHelper.IsDbg;
        IgnoreEscape = true;     // prevent WindowBase from messing things up.   
    }

    void OnLoaded(object s, RoutedEventArgs e) => themeSelector1.SetCurThemeToMenu(Thm); //dnf theming 2/2

    void OnWindowRestoree(object s, RoutedEventArgs e)
    {
        wr.Visibility = Visibility.Collapsed; wm.Visibility = Visibility.Visible; WindowState = WindowState.Normal;
    }
    void OnWindowMaximize(object s, RoutedEventArgs e)
    {
        wm.Visibility = Visibility.Collapsed; wr.Visibility = Visibility.Visible; WindowState = WindowState.Maximized;
    }

    async void OnSave(object s, RoutedEventArgs e) => await Task.Yield();
    async void OnAudio(object s, RoutedEventArgs e) => await Task.Yield();
    async void OnFlush(object s, RoutedEventArgs e) => await Task.Yield();
    void OnSettings(object s, RoutedEventArgs e) => MessageBox.Show("Under Construction...", "Under Construction...", MessageBoxButton.OK, MessageBoxImage.Information);

    void OnRequestNavigate(object s, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        e.Handled = true;
        if (s.GetType() != typeof(Hyperlink))
        {
            return;
        }

        if (Directory.Exists(e.Uri.ToString()))
        {
            _ = Process.Start("Explorer.exe", e.Uri.ToString());
        }
        else
        {
            _ = MessageBox.Show($"Directory  \n\n   {e.Uri}\n\ndoes not exist...\n\n...or is unaccessible at the moment ", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    void OnGoToLink(object s, RoutedEventArgs e) => _ = Process.Start("Explorer.exe", ((MenuItem)s)?.Tag?.ToString() ?? "C:\\");
    void OnUnchecked(object s, RoutedEventArgs e) => ((CheckBox)s).IsChecked = true;
    void OnHideGSReport(object s, RoutedEventArgs e) => Opener0.IsChecked = false;
}