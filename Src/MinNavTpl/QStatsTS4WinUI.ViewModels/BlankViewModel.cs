using CommunityToolkit.Mvvm.ComponentModel;

namespace QStatsTS4WinUI.ViewModels;

public partial class BlankViewModel : ObservableRecipient
{
    public BlankViewModel()
    {
    }

    [ObservableProperty] string hello = "Hello from VM Lib!";
}
