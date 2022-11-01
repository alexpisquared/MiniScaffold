namespace MinNavTpl.VM.VMs;
public partial class Page00VM : BaseDbVM
{
  readonly DateTimeOffset _now = DateTimeOffset.Now;
  public Page00VM(MainVM mainVM, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings usrStgns, AllowWriteDBStore allowWriteDBStore) : base(mainVM, lgr, cfg, bpr, sec, dbx, win, allowWriteDBStore, usrStgns, 8110) => _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      await Task.Delay(20);
      var sw = Stopwatch.StartNew();
      //await new WpfUserControlLib.Services.ClickOnceUpdater(Lgr).CopyAndLaunch(ReportProgress);
      //await ImportCsv();
      Lgr.Log(LogLevel.Trace, $"DB:  in {sw.ElapsedMilliseconds,8}ms  at SQL:{UserSetgs.PrefSrvrName} ▀▄▀▄▀▄▀▄▀");
      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();

  void ReportProgress(string msg)
  {
    ReportMessage = msg;
    Lgr.Log(LogLevel.Trace, msg);
  }

  [ObservableProperty] string reportMessage = ":0";

  [RelayCommand]
  async Task ImportCsv()
  {
    try
    {
      await Bpr.ClickAsync();

      await new CsvImporterService(Dbx, Lgr, _now).ImportCsvAsync(ReportProgress);
      await Bpr.TickAsync();
    }
    catch (Exception ex) { IsBusy = false; WriteLine(ex.Message); ex.Pop(Lgr); }
    finally { IsBusy = _saving = false; Bpr.Tick(); }
  }
}