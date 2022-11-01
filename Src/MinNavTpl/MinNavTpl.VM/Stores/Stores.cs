namespace MinNavTpl.Stores;

public class SrvrNameStore
{
  public event Action<string>? SrvrAdded;
  public event Action<string>? Changed;

  public void AddSvr(string name) => SrvrAdded?.Invoke(name);
  public void Change(string name) => Changed?.Invoke(name);
}
public class DtBsNameStore
{
  public event Action<string>? DtBsAdded;
  public event Action<string>? Changed;

  public void AddDBs(string name) => DtBsAdded?.Invoke(name);
  public void Change(string name) => Changed?.Invoke(name);
}
public class LetDbChgStore
{
  public event Action<bool>? Changed;
  public void Change(bool value) => Changed?.Invoke(value);
}

