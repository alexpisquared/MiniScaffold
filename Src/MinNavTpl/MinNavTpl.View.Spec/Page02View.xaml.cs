﻿namespace MinNavTpl.View.Spec;
public partial class Page02View : UserControl
{
  public Page02View() { InitializeComponent(); _ = tbFilter.Focus(); }
  void OnInitNewItem(object s, InitializingNewItemEventArgs e)
  {
    try
    {
      _ = dgPageCvs.Items.MoveCurrentToLast();

      if (dgPageCvs.SelectedItem != null)
        dgPageCvs.ScrollIntoView(dgPageCvs.SelectedItem);
    }
    catch (Exception ex) { ex.Pop(); }
  }
}
