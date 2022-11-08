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
public class EmailOfIStore
{
  public string LastVal { get; private set; } = ":Nul";
  public event Action<string, string?>? Changed; public void Change(string name, [CallerMemberName] string? cmn = "")
  {
    LastVal = name;
    Changed?.Invoke(name, cmn);
  }
}
public class LetDbChgStore
{
  public event Action<bool>? Changed; public void Change(bool value) => Changed?.Invoke(value);
}

