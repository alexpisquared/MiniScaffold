namespace MinNavTpl.Stores;

public class SrvrNameStore
{
  public event Action<string>? Added; public void Add(string name) => Added?.Invoke(name);
  public event Action<string>? Changed; public void Change(string name) => Changed?.Invoke(name);
}
public class DtBsNameStore
{
  public event Action<string>? Added; public void Add(string name) => Added?.Invoke(name);
  public event Action<string>? Changed; public void Change(string name) => Changed?.Invoke(name);
}
public class EmailOIStore
{
  public event Action<string>? Changed; public void Change(string name) => Changed?.Invoke(name);
}
public class LetDbChgStore
{
  public event Action<bool>? Changed; public void Change(bool value) => Changed?.Invoke(value);
}

