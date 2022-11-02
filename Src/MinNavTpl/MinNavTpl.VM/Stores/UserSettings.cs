﻿namespace MinNavTpl.VM.Stores;
public class UserSettings : UserSettingsBase // ..actually, not a store/store - just a concidence in naming.
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

    DtBsName = fromFile.DtBsName;
    SrvrName = fromFile.SrvrName;
    PrefDtBsRole = fromFile.PrefDtBsRole;
    PrefAplctnId = fromFile.PrefAplctnId;
    LetDbChg = fromFile.LetDbChg;
    IsAudible = fromFile.IsAudible;
    IsAnimeOn = fromFile.IsAnimeOn;

    _loaded = true;
  }
  string _r = "IpmUserRole";    /**/ public string PrefDtBsRole { get => _r; set { if (_r != value) { _r = value; SaveIfLoaded(); } } }
  string _s = ".\\sqlexpress";  /**/ public string SrvrName { get => _s; set { if (_s != value) { _s = value; SaveIfLoaded(); } } }
  string _d = "QStatsDbg";      /**/ public string DtBsName { get => _d; set { if (_d != value) { _d = value; SaveIfLoaded(); } } }
  bool _o;                      /**/ public bool LetDbChg { get => _o; set { if (_o != value) { _o = value; SaveIfLoaded(); } } }
  int _a = -2;                  /**/ public int PrefAplctnId { get => _a; set { if (_a != value) { _a = value; SaveIfLoaded(); } } }
  bool _u;                      /**/ public bool IsAudible { get => _u; set { if (_u != value) { _u = value; SaveIfLoaded(); } } }
  bool _n;                      /**/ public bool IsAnimeOn { get => _n; set { if (_n != value) { _n = value; SaveIfLoaded(); } } }

  void SaveIfLoaded() { if (_loaded) { LastSave = DateTimeOffset.Now; Save(this); } }
}