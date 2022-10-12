﻿namespace MinNavTpl.VM.Stores;
public class UserSettings : StandardLib.Base.UserSettingsStore_ // ..actually, not a store/store - just a concidence in naming.
{
  readonly bool _loaded;
  readonly ILogger? _logger;

  public UserSettings() => WriteLine("    UserSettings.Ctor(): Deserialized => Loading is done?");
  public UserSettings(ILogger lgr)
  {
    _logger = lgr;

    _logger.LogInformation("    UserSettings.Ctor(): Supplied by the DI => Loading here now...");

    if (_loaded) return;

    var fromFile = Load<UserSettings>();

    var dtoForThis = new MapperConfiguration(cfg => cfg.CreateMap<UserSettings, UserSettings>()).CreateMapper().Map<UserSettings>(fromFile); //not fun.

    PrefDtBsName = fromFile.PrefDtBsName;
    PrefSrvrName = fromFile.PrefSrvrName;
    PrefDtBsRole = fromFile.PrefDtBsRole;
    PrefAplctnId = fromFile.PrefAplctnId;
    AllowWriteDB = fromFile.AllowWriteDB;
    IsAudible = fromFile.IsAudible;
    IsAnimeOn = fromFile.IsAnimeOn;

    _loaded = true;
  }
  string _s = ".\\sqlexpress";  /**/ public string PrefSrvrName { get => _s; set { if (_s != value) { _s = value; SaveIf(); } } }
  string _d = "Inventory";      /**/ public string PrefDtBsName { get => _d; set { if (_d != value) { _d = value; SaveIf(); } } }
  string _r = "IpmUserRole";    /**/ public string PrefDtBsRole { get => _r; set { if (_r != value) { _r = value; SaveIf(); } } }
  bool _o;                      /**/ public bool AllowWriteDB { get => _o; set { if (_o != value) { _o = value; SaveIf(); } } }
  int _a = -2;                  /**/ public int PrefAplctnId { get => _a; set { if (_a != value) { _a = value; SaveIf(); } } }
  bool _u;                      /**/ public bool IsAudible { get => _u; set { if (_u != value) { _u = value; SaveIf(); } } }
  bool _n;                      /**/ public bool IsAnimeOn { get => _n; set { if (_n != value) { _n = value; SaveIf(); } } }

  void SaveIf() { if (_loaded) { LastSave = DateTimeOffset.Now; Save(this); } }
}