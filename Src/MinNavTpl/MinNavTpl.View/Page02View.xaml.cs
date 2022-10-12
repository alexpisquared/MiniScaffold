using DB.Inventory.Models;
using Microsoft.EntityFrameworkCore;
using MinimalNavTemplate.Contract;

namespace MinimalNavTemplate.View;
public partial class Page02View : UserControl
{
  const int _maxFrames = 25 * 60; // prevent from running forever 
  const double _periodSec = 1;
  CancellationTokenSource? _cts;
  bool _loaded, _isDirty;
  int _userid, _permid;
  Permission? _lastSelectPerm = null;
  User? _lastSelectUser = null;
  IPage02VMLtd? _vm;

  public Page02View() => InitializeComponent();
  void OnLoaded(object s, RoutedEventArgs e)
  {
    _vm = (IPage02VMLtd)DataContext;
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr.Tick();
    _loaded = true;

    _ = cbxApps.Focus();
  }
  async void DgPerm_SelectedCellsChanged(object s, SelectedCellsChangedEventArgs e)
  {
    if (!_loaded || e.AddedCells.Count < 1 || e.AddedCells[0].Column is not DataGridTextColumn) return;

    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr.Tick();

    _ = await SaveIfDirty("Sel Chd P");
    colPG.Visibility = Visibility.Collapsed;
    colUG.Visibility = Visibility.Visible;

    _lastSelectPerm = (Permission)e.AddedCells[0].Item;
    _permid = _lastSelectPerm.PermissionId;
    _userid = -1;

    _vm.DoDgSelChngdPerm(_lastSelectPerm);

    _lastSelectPerm.Selectd = true;

    dgPerm.Items.Refresh(); dgUser.Items.Refresh();
  }
  async void DgUser_SelectedCellsChanged(object s, SelectedCellsChangedEventArgs e)
  {
    if (!_loaded || e.AddedCells.Count < 1 || e.AddedCells[0].Column is not DataGridTextColumn) return;

    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr?.Tick();

    _ = await SaveIfDirty("Sel Chd U");
    colPG.Visibility = Visibility.Visible;
    colUG.Visibility = Visibility.Collapsed;

    _lastSelectUser = (User)e.AddedCells[0].Item;
    _userid = _lastSelectUser.UserIntId;
    _permid = -1;

    _vm.DoDgSelChngdUser(_lastSelectUser);

    _lastSelectUser.Selectd = true;
    dgUser.Items.Refresh(); dgPerm.Items.Refresh();
  }
  async void OnAddMe(object s, RoutedEventArgs e)
  {
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.DbxInv.Users.Local.Add(new User { UserId = $@"{Environment.UserDomainName}\{Environment.UserName}", AdminAccess = 0, Type = "U", Status = "A" });
    _isDirty = true;
    _ = await SaveIfDirty("SyncToSqlAdd Me", true);
    btnAddMe.Visibility = Visibility.Collapsed;
  }
  async void ChkIsPlaying_Checked(object s, RoutedEventArgs e)
  {
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr?.Tick();
    WriteLine($"-- <<<<<<<<<       {(_loaded ? "Starting" : "Not yet")}.");
    if (!_loaded) return;

    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_periodSec));
    await RunTimer(timer);
  }
  void ChkIsPlaying_Unchecked(object s, RoutedEventArgs e)
  {
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr?.Tick();
    WriteLine($"-- >>>>>>>>>       Cancelling   ."); _cts?.Cancel();
  }
  async void OnTogglePermission(object s, RoutedEventArgs e) => await ToggleRecalcAndSave((FrameworkElement)s);
  async void OnSave(object s, RoutedEventArgs e) => await SaveIfDirty("Manual");
  void OnChkDb(object s, RoutedEventArgs e) => _vm?.CheckDb();

  async Task RefreshEfAsync(bool isasync) //todo: move to VM.
  {
    var a = cbxApps.SelectedIndex;

    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    await _vm.LoadEF(isasync);

    cbxApps.SelectedIndex = a;

    for (var i = 0; i < dgUser.Items.Count; i++)
    {
      if (dgUser.Items[i] is User usr && usr.UserIntId == _userid)
      {
        dgUser.CurrentCell = new DataGridCellInfo(dgUser.Items[i], dgUser.Columns[0]);
        dgUser.SelectedCells.Add(dgUser.CurrentCell);
        break;
      }
    }

    for (var i = 0; i < dgPerm.Items.Count; i++)
    {
      if (dgPerm.Items[i] is Permission usr && usr.PermissionId == _permid)
      {
        dgPerm.CurrentCell = new DataGridCellInfo(dgPerm.Items[i], dgPerm.Columns[0]);
        dgPerm.SelectedCells.Add(dgPerm.CurrentCell);
        break;
      }
    }
  }
  async Task RunTimer(PeriodicTimer timer)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));

      var counter = 0;
      _cts = new CancellationTokenSource();
      while (_cts is not null && await timer.WaitForNextTickAsync(_cts.Token))
      {
        if (counter > _maxFrames)
        {
          WriteLine($"-- Cancelling at counter {counter}   .");
          chkIsPlaying.IsChecked = false;
          return;
        }

        chkIsPlaying.Background = ++counter % 2 == 0 ? Brushes.Yellow : Brushes.LightPink;

        if (_permid > 0)
        {
          var ar = await _vm.DbxInv.PermissionAssignments.Where(r => r.PermissionId == _permid).CountAsync();
          var al = _vm.DbxInv.PermissionAssignments.Local.Where(r => r.PermissionId == _permid).Count();
          if (ar != al)
            await RefreshEfAsync(false);
        }
        else if (_userid > 0)
        {
          var ar = await _vm.DbxInv.PermissionAssignments.Where(r => r.UserId == _userid).CountAsync();
          var al = _vm.DbxInv.PermissionAssignments.Local.Where(r => r.UserId == _userid).Count();
          if (ar != al)
            await RefreshEfAsync(false);
        }

        if (_cts?.Token.IsCancellationRequested == true) // Poll on this property if you have to do other cleanup before throwing.
        {
          _vm.Logger.LogWarning($"║   PeriodicTimer: -- CancellationRequested => Cancelling timer.");
          // Clean up here, then...
          _cts.Token.ThrowIfCancellationRequested();

          chkIsPlaying.Background = Brushes.White;
        }
      }
    }
    catch (TaskCanceledException ex) /**/ { WriteLine($"-- {ex.GetType().Name}: \t{ex.Message}.   "); Hand.Play(); }
    catch (OperationCanceledException ex) { WriteLine($"-- {ex.GetType().Name}: \t{ex.Message}.   "); Hand.Play(); }
    //catch (OperationCanceledException ex) { Logger.LogWarning(ex.Message); }
    catch (Exception ex)             /**/ { WriteLine($"-- {ex.GetType().Name}: \t{ex.Message}.   "); Hand.Play(); }
    finally { _cts?.Dispose(); WriteLine($"-- finally      .\n"); }
  }
  async Task<string> SaveIfDirty(string note, bool skipUdate = false)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));

      if (!_isDirty)
      {
        _vm.Logger.LogInformation($"{note}    No changes");
        return "No changes to save";
      }

      try
      {
        if (!skipUdate)
        {
          if (_vm is not null) _vm.IsBusy = true;
          UpdateCrosRefTable();
        }

#if SaveForDevOnly //         if (true)// DevOps.IsDevMachineH || new[] { ".", @"mtUATsqldb" }.Contains(cbxSrvr.SelectedValue))
      MessageBox.Show(this, "Press any key to continue...\n\n\t...or any other key to quit", "Changes Saved ...NOT!!! (SaveForDevOnly is ON) :(", MessageBoxButton.OK, MessageBoxImage.Information);
#else
        ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
        var report = await _vm.SaveLogReportOrThrow(_vm.DbxInv, $"SkippUpd:{skipUdate,5}"); // var (success, rowsSavedCnt, report) = await _vm.DbxInv.TrySaveReportAsync($"SkippUpd:{skipUdate,5}");
#endif

        //if (rowsSavedCnt > 0) 
        _vm.Logger?.LogInformation($"{note}    {report}");

        _isDirty = false;
        return report;
      }
      catch (Exception ex) { ex.Pop(lgr: _vm.Logger); return ex.Message; }
    }
    finally { if (_vm is not null) _vm.IsBusy = false; }
  }
  void UpdateCrosRefTable()
  {
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    WriteLine("    Upd xRef  " +
      $"G:{_vm.DbxInv.Permissions.Local.Where(r => r.Granted == true).Count()}  +  " +
      $"f:{_vm.DbxInv.Permissions.Local.Where(r => r.Granted == false).Count()}  +  " +
      $"n:{_vm.DbxInv.Permissions.Local.Where(r => r.Granted is null).Count()}  =  " +
      $"{_vm.DbxInv.Permissions.Local.Count}");

    if (_userid > 0 && _permid < 0)
    {
#if false // no difference
      _context.Permissions.Local.ToList().ForEach(p =>
      {
        var dbpa = _context.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == _userid && r.PermissionId == p.PermissionId);
        if (dbpa != null)
        {
          if (p.Granted == true)
            dbpa.Status = "G";
          else
            _context.PermissionAssignments.Local.Remove(dbpa);
        }
        else if (p.Granted == true)
          _context.PermissionAssignments.Local.Add(new PermissionAssignment { UserId = _userid, PermissionId = p.PermissionId, Status = "G" });
      });
#else
      _vm.DbxInv.Permissions.Local.Where(r => r.Granted == true).ToList().ForEach(p =>
      {
        var dbpa = _vm.DbxInv.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == _userid && r.PermissionId == p.PermissionId);
        if (dbpa != null)
        {
          if (dbpa.Status != "G")
            dbpa.Status = "G";
        }
        else
          _vm.DbxInv.PermissionAssignments.Local.Add(new PermissionAssignment { UserId = _userid, PermissionId = p.PermissionId, Status = "G" });
      });

      _vm.DbxInv.Permissions.Local.Where(r => r.Granted == false).ToList().ForEach(p =>
      {
        var dbpa = _vm.DbxInv.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == _userid && r.PermissionId == p.PermissionId);
        if (dbpa != null)
          _vm.DbxInv.PermissionAssignments.Local.Remove(dbpa);
      });
#endif
    }
    else if (_userid < 0 && _permid > 0)
    {
      _vm.DbxInv.Users.Local.ToList().ForEach(u =>
      {
        var dbpa = _vm.DbxInv.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == u.UserIntId && r.PermissionId == _permid);
        if (dbpa != null)
        {
          if (u.Granted == true)
            dbpa.Status = "G";
          else
            _vm.DbxInv.PermissionAssignments.Local.Remove(dbpa);
        }
        else if (u.Granted == true)
          _vm.DbxInv.PermissionAssignments.Local.Add(new PermissionAssignment { UserId = u.UserIntId, PermissionId = _permid, Status = "G" });
      });
    }
  }
  internal async Task ToggleRecalcAndSave(FrameworkElement src)
  {
    ArgumentNullException.ThrowIfNull(_vm, nameof(_vm));
    _vm.Bpr.Click();

    var dc = ((FrameworkElement)src.TemplatedParent).DataContext;
    if (dc is Permission perm && _userid > 0)
    {
      var dbpa = _vm.DbxInv.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == _userid && r.PermissionId == perm.PermissionId);
      if (dbpa == null && perm.Granted == true)
      {
        var pa = new PermissionAssignment
        {
          UserId = _userid,
          PermissionId = perm.PermissionId,
          Status = "G",
          Permission = perm,
          User = _lastSelectUser ?? throw new ArgumentNullException(nameof(src))
        };
        _vm.DbxInv.PermissionAssignments.Local.Add(pa);
        _isDirty = true;
        _ = await _vm.SyncToSqlAdd(pa);
      }

      if (dbpa != null && perm.Granted == false)
      {
        _ = _vm.DbxInv.PermissionAssignments.Local.Remove(dbpa);
        _isDirty = true;
        _ = await _vm.SyncToSqlRmv(dbpa);
      }
    }
    else if (dc is User user && _permid > 0)
    {
      var dbpa = _vm.DbxInv.PermissionAssignments.Local.FirstOrDefault(r => r.UserId == user.UserIntId && r.PermissionId == _permid);
      if (dbpa == null && user.Granted == true)
      {
        var pa = new PermissionAssignment
        {
          UserId = user.UserIntId,
          PermissionId = _permid,
          Status = "G",
          Permission = _lastSelectPerm ?? throw new ArgumentNullException(nameof(src)),
          User = user
        };
        _vm.DbxInv.PermissionAssignments.Local.Add(pa);
        _isDirty = true;
        _ = await _vm.SyncToSqlAdd(pa);
      }

      if (dbpa != null && user.Granted == false)
      {
        _ = _vm.DbxInv.PermissionAssignments.Local.Remove(dbpa);
        _isDirty = true;
        _ = await _vm.SyncToSqlRmv(dbpa);
      }
    }

    _ = await SaveIfDirty(nameof(ToggleRecalcAndSave), true);
  }
  void OnUnderConstruction(object s, RoutedEventArgs e) => _ = MessageBox.Show("Under Construction...", "Under Construction...", MessageBoxButton.OK, MessageBoxImage.Information);
}