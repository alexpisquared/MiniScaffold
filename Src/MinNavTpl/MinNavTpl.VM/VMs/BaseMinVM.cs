namespace MinNavTpl.VM.VMs;
public class BaseMinVM : ObservableValidator, IDisposable
{
  protected bool _ctored;
  protected bool _loaded;
  //public BaseMinVM()  {    _ctored = true;  }
  public virtual async Task<bool> InitAsync() { /*WriteLine($"::> Init of {GetType().Name}");*/ await Task.Yield(); _loaded = true; return true; }
  public virtual async Task<bool> WrapAsync() { /*WriteLine($"::> Wrap of {GetType().Name}");*/ await Task.Yield(); return true; }

  bool _disposedValue;
  protected virtual void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        //todo: stores:     YearOfInterestStore.YearChanged -= YearOfInterestStore_YearChanged;
        // TODO: dispose managed state (managed objects)
      }

      // TODO: free unmanaged resources (unmanaged objects) and override finalizer
      // TODO: set large fields to null

      _disposedValue = true;
    }
  }
  // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
  // ~BaseDbVM()
  // {
  //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
  //     Dispose(disposing: false);
  // }
  public virtual void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  protected string? GetCaller([CallerMemberName] string? cmn = "") => cmn;
}