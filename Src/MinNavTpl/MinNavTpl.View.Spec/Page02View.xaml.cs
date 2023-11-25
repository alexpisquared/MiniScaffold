using System.Collections;

namespace MinNavTpl.View.Spec;
public partial class Page02View : UserControl
{
    public Page02View()
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
}

public static class DataGridExtensions
{
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
        "SelectedItems",
        typeof(IList),
        typeof(DataGridExtensions),
        new PropertyMetadata(null, OnSelectedItemsChanged));

    public static IList GetSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SelectedItemsProperty);
    }

    public static void SetSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dataGrid = (DataGrid)d;
        dataGrid.SelectionChanged += (s, args) =>
        {
            var selectedItems = GetSelectedItems(dataGrid);
            selectedItems.Clear();
            if (dataGrid.SelectedItems != null)
            {
                foreach (var item in dataGrid.SelectedItems)
                    selectedItems.Add(item);
            }
        };
    }
}
