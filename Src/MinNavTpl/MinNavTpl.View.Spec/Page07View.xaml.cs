namespace MinNavTpl.View.Spec;
public partial class Page07View : UserControl
{
    public Page07View()
    {
        InitializeComponent(); Loaded += async (s, e) => { await Task.Delay(2500); _ = tbFilter.Focus(); };
    }
    void OnInitNewItem(object s, InitializingNewItemEventArgs e)
    {
        try
        {
            _ = ((DataGrid)s).Items.MoveCurrentToLast();

            if (((DataGrid)s).SelectedItem != null)
                ((DataGrid)s).ScrollIntoView(((DataGrid)s).SelectedItem);
        }
        catch (Exception ex) { ex.Pop(); }
    }
    void dgPage_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        try
        {
            if (((DataGrid)s).SelectedItem != null)
                ((DataGrid)s).ScrollIntoView(((DataGrid)s).SelectedItem);
        }
        catch (Exception ex) { ex.Pop(); }
    }
    void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.Items.Count > 0) dataGrid.SelectedIndex = -1;
    }
}
