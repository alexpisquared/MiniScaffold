namespace MinNavTpl.VM.VMs;

public class LayoutVM : BaseMinVM
{
  public LayoutVM(NavBarVM navBarVM, BaseMinVM contentVM)
  {
    NavBarVM = navBarVM;
    ContentVM = contentVM;
  }

  public NavBarVM NavBarVM { get; }
  public BaseMinVM ContentVM { get; }

  public override void Dispose()
  {
    NavBarVM.Dispose();
    ContentVM.Dispose();

    base.Dispose();
  }
}
