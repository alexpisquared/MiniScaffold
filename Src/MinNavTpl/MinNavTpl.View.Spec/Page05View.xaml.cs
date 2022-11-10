﻿namespace MinNavTpl.View.Spec;
public partial class Page05View : UserControl
{
  public Page05View()  {    InitializeComponent();    _ = tbFilter.Focus();  }
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
