namespace MinNavTpl.VM.VMs;
public class Page03VM : BaseDbVM
{
  readonly string _constr;

  public Page03VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110)
  {
    BmsIntegration = false;
    _constr = string.Format(Config[GenConst.SqlVerSpm] ?? "!d", UserSetgs.PrefSrvrName, UserSetgs.PrefDtBsName);
  }
  public override async Task<bool> InitAsync() { await InitAsyncLocal(); _ = await base.InitAsync(); return true; }
  async Task InitAsyncLocal() => await Task.Yield();
  public override async Task RefreshReloadAsync([CallerMemberName] string? cmn = "")
  {
    WriteLine($"TrWL:> ###### Caller> {cmn}->Page03VM.RefreshReloadAsync() ");
    await InitAsyncLocal(); // refresh on YOI change.
    await base.RefreshReloadAsync();
  }

  bool _b; public bool BmsIntegration { get => _b; set => SetProperty(ref _b, value); }
  bool _a; public bool FilterAdmn { get => _a; set { if (SetProperty(ref _a, value)) { ADUserCVS?.Refresh(); } } }
  bool _w; public bool FilterWork { get => _w; set { if (SetProperty(ref _w, value)) { ADUserCVS?.Refresh(); } } }
  bool _r; public bool FilterRead { get => _r; set { if (SetProperty(ref _r, value)) { ADUserCVS?.Refresh(); } } }
  bool _l; public bool FilterLogn { get => _l; set { if (SetProperty(ref _l, value)) { ADUserCVS?.Refresh(); } } }
  string _xh = ""; public string SearchText { get => _xh; set { if (SetProperty(ref _xh, value)) ADUserCVS?.Refresh(); } }
  ADUser? _su; public ADUser? SelectUsr { get => _su; set { _ = SetProperty(ref _su, value); if (_su != null) WriteLine($"TrWL:> Select User:  {_su.FullName,-26} {_su.A,-6}{_su.W,-6}{_su.R,-6}{_su.L,-6}  {_su.Permisssions}"); } }
  ICollectionView? _cv1; public ICollectionView? ADUserCVS { get => _cv1; set => SetProperty(ref _cv1, value); }

  IRelayCommand? _ca; public IRelayCommand SetToAdmnCommand => _ca ??= new AsyncRelayCommand<object>(SetToAdmn); async Task<int> SetToAdmn(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.IpmAdmin);
  IRelayCommand? _cp; public IRelayCommand SetToUserCommand => _cp ??= new AsyncRelayCommand<object>(SetToUser); async Task<int> SetToUser(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.WorkUser);
  IRelayCommand? _cr; public IRelayCommand SetToReadCommand => _cr ??= new AsyncRelayCommand<object>(SetToRead); async Task<int> SetToRead(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.ReadOnly);
  IRelayCommand? _cn; public IRelayCommand SetToNoneCommand => _cn ??= new AsyncRelayCommand<object>(SetToNone); async Task<int> SetToNone(object? isAddObj) => await SetToRole(isAddObj, IpmUserRole.LognOnly);
  async Task<int> SetToRole(object? isAddObj, string dbRole) { await Task.Yield(); return 1; }
}