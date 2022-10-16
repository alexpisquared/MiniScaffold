namespace MinNavTpl.VM.VMs;
public class Page01VM : BaseDbVM
{
  public Page01VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, SrvrStore srvrStore, DtBsStore dtbsStore, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110)
  {
    SrvrStore = srvrStore; SrvrStore.CurrentSrvrChanged += SrvrStore_SrvrChngd;
    DtBsStore = dtbsStore; DtBsStore.CurrentDtbsChanged += DtbsStore_DtbsChngd;

    _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Logger); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
  }

  public SrvrStore SrvrStore { get; }
  public DtBsStore DtBsStore { get; }
  async void SrvrStore_SrvrChngd(ADSrvr srvr) { SelectSrvr = srvr; await RefreshReloadAsync(); }
  async void DtbsStore_DtbsChngd(ADDtBs srvr) { SelectDtBs = srvr; await RefreshReloadAsync(); }

  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      var swAD = Stopwatch.StartNew();
      await Task.Yield();
      return true;
    }
    catch (Exception ex) { ex.Pop(Logger); return false; }
    finally
    {
      IsBusy = false;
      _ = await base.InitAsync();
    }
  }
  public override Task<bool> WrapAsync()
  {
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    return base.WrapAsync();
  }

  ADSrvr? _cs; public ADSrvr? SelectSrvr
  {
    get => _cs; set
    {
      if (SetProperty(ref _cs, value) && value is not null && _inited)
      {
        try
        {
          Bpr.Click();          //ArgumentNullException.ThrowIfNull(value, nameof(value));
          UserSetgs.PrefSrvrName = value.Name;
          SrvrStore.ChgSrvr(value);
#if true
          _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
          _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
          {
            await Task.Delay(2600);
            System.Windows.Application.Current.Shutdown();
          });
#else
          Page01VMHelpers.LoadServerDBsFAF(value.Name, DtBsList, SetLoadedFlag, string.Format(Config[GenConst.SqlVer] ?? "d", value.Name, _startUpDB), Logger);
#endif
        }
        catch (Exception ex) { ex.Pop(Logger); }
      }
    }
  }
  ADDtBs? _cd; public ADDtBs? SelectDtBs
  {
    get => _cd; set
    {
      if (SetProperty(ref _cd, value) && value is not null && _inited)
      {
        Bpr.Click();
        UserSetgs.PrefDtBsName = value.Name;
        DtBsStore.ChgDtBs(value);
        //Page01VMHelpers.LoadDtBsRolesFAF(value.Name, RoleList, _spm, string.Format(Config[GenConst.SqlVerSpm] ?? "'", SelectSrvr?.Name, SelectDtBs?.Name), Logger);
      }
    }
  }
  ADUser? _ct; public ADUser? CurentUser { get => _ct; set { if (SetProperty(ref _ct, value) && value is not null) { WriteLine($"TrWL:> Curent User:  {value.FullName,-26} {value.A,-6}{value.W,-6}{value.R,-6}{value.L,-6}  {value.Permisssions}"); } } }

  public override void Dispose()
  {
    SrvrStore.CurrentSrvrChanged -= SrvrStore_SrvrChngd;
    DtBsStore.CurrentDtbsChanged -= DtbsStore_DtbsChngd;

    base.Dispose();
  }
}