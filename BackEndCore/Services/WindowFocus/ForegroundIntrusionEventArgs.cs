using System;
using System.Collections.Generic;
using System.Text;

namespace BackEndCore.Services.WindowFocus;

public sealed class ForegroundIntrusionEventArgs : EventArgs
{
    public string ProcessName { get; init; } = "";
    public string WindowTitle { get; init; } = "";
}
