using Microsoft.UI.Xaml.Controls;

using QStatsTS4WinUI.ViewModels;

namespace QStatsTS4WinUI.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class DataGrid2Page : Page
{
    public DataGrid2ViewModel ViewModel
    {
        get;
    }

    public DataGrid2Page()
    {
        ViewModel = App.GetService<DataGrid2ViewModel>();
        InitializeComponent();
    }
}
