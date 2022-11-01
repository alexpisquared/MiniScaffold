namespace MinNavTpl.VM.VMs;
public class Page03VM : BaseDbVM
{
  public Page03VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, dbx, win, allowWriteDBStore, usrStgns, 8110)
  {
  }
  public override async Task<bool> InitAsync() { await InitAsyncLocal(); _ = await base.InitAsync(); return true; }  async Task InitAsyncLocal() => await Task.Yield();

  string reportMessage="";  public string ReportMessage { get => reportMessage; set => SetProperty(ref reportMessage, value); }
}