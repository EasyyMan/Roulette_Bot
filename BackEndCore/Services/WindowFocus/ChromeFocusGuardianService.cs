using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BackEndCore.Services.WindowFocus;

public sealed class ChromeFocusGuardianService : IChromeFocusGuardianService
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const string ChromeWindowClass = "Chrome_WidgetWin_1";

    private WinEventDelegate? _hookDelegate; // kept alive so the GC doesn't collect the callback
    private IntPtr _hookHandle = IntPtr.Zero;
    private HashSet<string> _allowedProcessNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string>? _log;

    public event EventHandler<ForegroundIntrusionEventArgs>? IntrusionDetected;

    public ChromeFocusGuardianService(Action<string>? log = null) => _log = log;

    public void Start(IReadOnlyCollection<string>? extraAllowedProcessNames = null)
    {
        _allowedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome",
            Process.GetCurrentProcess().ProcessName // your WPF bot itself
        };

        if (extraAllowedProcessNames != null)
            foreach (var name in extraAllowedProcessNames) _allowedProcessNames.Add(name);

        _hookDelegate = OnForegroundChanged;
        _hookHandle = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _hookDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero) { UnhookWinEvent(_hookHandle); _hookHandle = IntPtr.Zero; }
    }

    private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;

        GetWindowThreadProcessId(hwnd, out uint pid);
        string processName;
        try { processName = Process.GetProcessById((int)pid).ProcessName; }
        catch { return; } // process already gone (toast that auto-closed, etc.)

        if (_allowedProcessNames.Contains(processName)) return; // expected — ignore

        var titleBuf = new StringBuilder(256);
        GetWindowText(hwnd, titleBuf, titleBuf.Capacity);

        _log?.Invoke($"[FocusGuardian] Intrusion: {processName} — \"{titleBuf}\"");
        IntrusionDetected?.Invoke(this, new ForegroundIntrusionEventArgs
        {
            ProcessName = processName,
            WindowTitle = titleBuf.ToString()
        });

        // Shove focus back to Chrome
        IntPtr chromeHwnd = FindWindow(ChromeWindowClass, null);
        if (chromeHwnd != IntPtr.Zero) SetForegroundWindow(chromeHwnd);
    }

    public bool IsClickTargetClear(int screenX, int screenY)
    {
        IntPtr hwndAtPoint = WindowFromPoint(new POINT { X = screenX, Y = screenY });
        IntPtr root = GetAncestor(hwndAtPoint, 2 /* GA_ROOT */);
        var sb = new StringBuilder(256);
        GetClassName(root, sb, sb.Capacity);
        return sb.ToString() == ChromeWindowClass;
    }

    public void Dispose() => Stop();

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
}