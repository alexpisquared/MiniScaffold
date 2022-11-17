﻿namespace MinNavTpl.View.Spec;
public partial class Page02View : UserControl
{
  public Page02View() { InitializeComponent(); Loaded += async (s, e) => { await Task.Delay(2500); _ = tbFilter.Focus(); }; }
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
