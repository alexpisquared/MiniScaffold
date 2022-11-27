using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class DataGrid3Page : Page
{
    public DataGrid3ViewModel ViewModel
    {
        get;
    }

    public DataGrid3Page()
    {
        ViewModel = App.GetService<DataGrid3ViewModel>();
        InitializeComponent();
    }
}
