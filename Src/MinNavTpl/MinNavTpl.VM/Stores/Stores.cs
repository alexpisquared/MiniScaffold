namespace MinNavTpl.Stores;

public class SrvrNameStore
{
  public event Action<ADSrvr>? SrvrAdded;
  public event Action<ADSrvr>? CurrentSrvrChanged;

  public void AddSrvr(ADSrvr name) => SrvrAdded?.Invoke(name);
  public void ChgSrvr(ADSrvr name) => CurrentSrvrChanged?.Invoke(name);
}
public class DtBsNameStore
{
  public event Action<ADDtBs>? DtBsAdded;
  public event Action<ADDtBs>? CurrentDtbsChanged;

  public void AddDtBs(ADDtBs name) => DtBsAdded?.Invoke(name);
  public void ChgDtBs(ADDtBs name) => CurrentDtbsChanged?.Invoke(name);
}
public class LetDbChgStore
{
  public event Action<bool>? AllowWriteDBChanged;

  public void ChangAllowWriteDB(bool value) => AllowWriteDBChanged?.Invoke(value);
}

