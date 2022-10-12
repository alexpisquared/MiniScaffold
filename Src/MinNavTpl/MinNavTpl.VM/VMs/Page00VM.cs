using MinNavTpl.VM.Stores;

namespace MinNavTpl.VM.VMs;
public class Page00VM : BaseDbVM
{
  public Page00VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, InventoryContext inv, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, inv, win, allowWriteDBStore, usrStgns, 8110) => _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Logger); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter

  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      await Task.Delay(2000);
      var sw = Stopwatch.StartNew();
      await new WpfUserControlLib.Services.ClickOnceUpdater(Logger).CopyAndLaunch(ReportProgress);
      Logger.Log(LogLevel.Trace, $"DB:  in {sw.ElapsedMilliseconds,8}ms  at SQL:{UserSetgs.PrefSrvrName} ▀▄▀▄▀▄▀▄▀");
      return true;
    }
    catch (Exception ex) { ex.Pop(Logger); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();

  void ReportProgress(string msg) => ReportMessage = msg;
  string _bm = "Hang on...\n\n       ...updating the binaries.\n\n              Will relaunch when done."; public string ReportMessage { get => _bm; set => SetProperty(ref _bm, value); }

  public override void Dispose() => base.Dispose();
}