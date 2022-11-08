namespace MinNavTpl.View.Spec;

public partial class Page01View : UserControl
{
  public Page01View() { InitializeComponent(); _ = tbFilter.Focus(); }
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
