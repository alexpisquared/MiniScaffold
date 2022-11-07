using MinNavTpl.Stores;

namespace MinNavTpl.VM.VMs;
public class EmailDetailVM : BaseDbVM
{
  public EmailDetailVM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, EmailOfIStore eml, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110)
  {
    EmaiStore = eml; EmaiStore.Changed += EmaiStore_Chngd;
    EmailOfIProp = eml.LastVal;
  }
  public override async Task<bool> InitAsync() { await InitAsyncTask(); _ = await base.InitAsync(); return true; }
  async Task InitAsyncTask() => await Task.Yield();
  public override void Dispose()  {
    EmaiStore.Changed -= EmaiStore_Chngd;
    base.Dispose();  }

  void EmaiStore_Chngd(string val) { EmailOfIProp = val;   /* await RefreshReloadAsync(); */ }

  public EmailOfIStore EmaiStore { get; }

  string _em="°"; public string EmailOfIProp { get => _em; set => SetProperty(ref _em, value); }

}