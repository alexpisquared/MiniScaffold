namespace MinNavTpl.VM.Services;

public interface ICompositeNavSvc : INavSvc { }

public class CompositeNavSvc : ICompositeNavSvc
{
  readonly IEnumerable<INavSvc> _navigationServices;

  public CompositeNavSvc(params INavSvc[] navigationServices) => _navigationServices = navigationServices;

  public void Navigate() => _navigationServices.ToList().ForEach(navigationService => navigationService.Navigate());
}
