namespace MinNavTpl.VM.VMs;
public class Page02VM : BaseDbVM//, IPage02VMLtd
{
  public Page02VM(MainVM mainVM, INavSvc loginNavSvc, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, dbx, win, allowWriteDBStore, usrStgns, 8110)
  {
  }
}