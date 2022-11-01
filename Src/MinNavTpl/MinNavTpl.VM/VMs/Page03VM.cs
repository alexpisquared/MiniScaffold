namespace MinNavTpl.VM.VMs;
public class Page03VM : BaseDbVM
{
  public Page03VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110) { }
  public override async Task<bool> InitAsync() { await InitAsyncLocal(); _ = await base.InitAsync(); return true; }
  async Task InitAsyncLocal() => await Task.Yield();
}