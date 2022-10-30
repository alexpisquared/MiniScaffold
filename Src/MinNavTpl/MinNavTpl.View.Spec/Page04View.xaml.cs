namespace MinNavTpl.View.Spec;
public partial class Page04View : UserControl
{
  public Page04View() => InitializeComponent();
  void onInitNewItem(object s, InitializingNewItemEventArgs e)
  {
    try
    {
      //_ = dgLeads.items.MoveCurrentToLast();

      if (dgLeads.SelectedItem != null)
        dgLeads.ScrollIntoView(dgLeads.SelectedItem);

      _ = tbxNote.Focus();
    }
    catch (Exception ex) { ex.Pop(); }
  }
}
