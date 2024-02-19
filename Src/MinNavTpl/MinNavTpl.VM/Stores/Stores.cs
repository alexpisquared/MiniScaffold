namespace MinNavTpl.Stores;

public class SrvrNameStore { public event Action<string>? Changed; public void Change(string val) => Changed?.Invoke(val); public event Action<string>? Added; public void Add(string val) => Added?.Invoke(val); }
public class DtBsNameStore { public event Action<string>? Changed; public void Change(string val) => Changed?.Invoke(val); }
public class GSReportStore { public event Action<string>? Changed; public void Change(string val) => Changed?.Invoke(val); }
public class LetDbChgStore { public event Action<bool>? Changed; public void Change(bool val) => Changed?.Invoke(val); }
public class IsBusy__Store { public event Action<bool>? Changed; public void Change(bool val) => Changed?.Invoke(val); }
public class EmailOfIStore
{
  public string LastVal { get; private set; } = ":Nul";
  public event Action<string, string?>? Changed; public void Change(string val, [CallerMemberName] string? cmn = "")
  {
    LastVal = val;
    Changed?.Invoke(val, cmn);
  }
}

