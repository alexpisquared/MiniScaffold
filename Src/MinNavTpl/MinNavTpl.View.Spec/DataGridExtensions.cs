using System.Collections;

namespace MinNavTpl.View.Spec;

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
