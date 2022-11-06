namespace MinNavTpl.View.Spec;
public partial class Page04View : UserControl
{
  public Page04View()  {    InitializeComponent();    _ = tbFilter.Focus();  }
  void OnInitNewItem(object s, InitializingNewItemEventArgs e)
  {
    try
    {
      _ = dgLeads.Items.MoveCurrentToLast();

      if (dgLeads.SelectedItem != null)
        dgLeads.ScrollIntoView(dgLeads.SelectedItem);
    }
    catch (Exception ex) { ex.Pop(); }
  }
}