namespace BackEndCore.Services.WindowFocus;

public interface IChromeFocusGuardianService : IDisposable
{
    void Start(IntPtr seleniumHwnd, IReadOnlyCollection<string>? extraAllowedProcessNames = null);
    void Stop();
    bool IsClickTargetClear(int screenX, int screenY);

    event EventHandler<ForegroundIntrusionEventArgs>? IntrusionDetected;
}