using System;
using System.Collections.Generic;
using System.Text;

namespace BackEndCore.Services.WindowFocus;

public interface IChromeFocusGuardianService : IDisposable
{
    void Start(IReadOnlyCollection<string>? extraAllowedProcessNames = null);
    void Stop();
    bool IsClickTargetClear(int screenX, int screenY);

    event EventHandler<ForegroundIntrusionEventArgs>? IntrusionDetected;
}