﻿namespace MinNavTpl.VM.VMs;
public partial class Page00VM : BaseDbVM
{
  readonly DateTimeOffset _now = DateTimeOffset.Now;
  public Page00VM(MainVM mvm, ILogger lgr, IConfigurationRoot cfg, IBpr bpr, ISecForcer sec, QStatsRlsContext dbx, IAddChild win, UserSettings stg, SrvrNameStore svr, DtBsNameStore dbs, LetDbChgStore awd) : base(mvm, lgr, cfg, bpr, sec, dbx, win, svr, dbs, awd, stg, 8110) => _ = Application.Current.Dispatcher.InvokeAsync(async () => { try { await Task.Yield(); } catch (Exception ex) { ex.Pop(Lgr); } });    //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
  public override async Task<bool> InitAsync()
  {
    try
    {
      IsBusy = true;
      await Task.Delay(200);
      var sw = Stopwatch.StartNew();

      Cfg[CfgName.ServerLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => SqlServrs.Add(r));
      Cfg[CfgName.DtBsNmLst]?.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(r => DtBsNames.Add(r));

      SqlServr = UsrStgns.PrefSrvrName;
      DtBsName = UsrStgns.PrefDtBsName;

      //await new WpfUserControlLib.Services.ClickOnceUpdater(Lgr).CopyAndLaunch(ReportProgress);
      //await ImportCsv();

      Lgr.Log(LogLevel.Trace, $"DB:  in {sw.ElapsedMilliseconds,8}ms  at SQL:{UsrStgns.PrefSrvrName} ▀▄▀▄▀▄▀▄▀");
      return true;
    }
    catch (Exception ex) { ex.Pop(Lgr); return false; }
    finally { _ = await base.InitAsync(); }
  }
  public override Task<bool> WrapAsync() => base.WrapAsync();
  public override void Dispose() => base.Dispose();


  public List<string> SqlServrs { get; } = new();
  public List<string> DtBsNames { get; } = new();
  string _qs = default!; public string SqlServr
  {
    get => _qs; set
    {
      if (SetProperty(ref _qs, value, true) && value is not null && _loaded)
      {
        Bpr.Click();

        UsrStgns.PrefSrvrName = value;

        _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
        _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
        {
          await Task.Delay(2600);
          Application.Current.Shutdown();
        });
      }
    }
  }
  string _dn = default!; public string DtBsName
  {
    get => _dn; set
    {
      if (SetProperty(ref _dn, value, true) && value is not null && _loaded)
      {
        Bpr.Click();

        UsrStgns.PrefSrvrName = value;

        _ = Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? "Notepad.exe"));
        _ = Application.Current.Dispatcher.InvokeAsync(async () => //tu: async prop - https://stackoverflow.com/questions/6602244/how-to-call-an-async-method-from-a-getter-or-setter
        {
          await Task.Delay(2600);
          Application.Current.Shutdown();
        });
      }
    }
  }

  void ReportProgress(string msg)
  {
    Report = msg;
    Lgr.Log(LogLevel.Trace, msg);
  }

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