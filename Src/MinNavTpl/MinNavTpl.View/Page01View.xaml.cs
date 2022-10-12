namespace MinNavTpl.View;

public partial class Page01View : UserControl
{
  public Page01View()
  {
    InitializeComponent();

    Loaded += async (s, e) => { await Task.Delay(1500)/*!!.ConfigureAwait(false)*/; _ = S.Focus(); };
  }
}
