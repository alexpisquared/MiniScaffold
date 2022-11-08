namespace MinNavTpl.View.Spec;
public partial class EmailDetailView : UserControl
{
  public EmailDetailView() { InitializeComponent(); _ = tbFilter.Focus(); }
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
