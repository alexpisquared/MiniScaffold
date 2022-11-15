﻿namespace MinNavTpl.View.Spec;

public partial class Page01View : UserControl
{
  public Page01View() { InitializeComponent(); _ = tbFilter.Focus(); }
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

  void Button_Click(object sender, RoutedEventArgs e)
  {
    //dgPage.Refresh();
  }
}
