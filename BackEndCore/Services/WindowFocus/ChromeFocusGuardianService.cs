using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BackEndCore.Services.WindowFocus;

public sealed class ChromeFocusGuardianService : IChromeFocusGuardianService
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private WinEventDelegate? _hookDelegate;
    private IntPtr _hookHandle = IntPtr.Zero;
    private IntPtr _targetChromeHwnd = IntPtr.Zero; // Stores the specific Selenium window
    private HashSet<string> _allowedProcessNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string>? _log;

    public event EventHandler<ForegroundIntrusionEventArgs>? IntrusionDetected;

    public ChromeFocusGuardianService(Action<string>? log = null)
    {
        _log = log;
    }

    public void Start(IntPtr seleniumHwnd, IReadOnlyCollection<string>? extraAllowedProcessNames = null)
    {
        _targetChromeHwnd = seleniumHwnd;

        // Cache allowed processes
        _allowedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome"
        };

        using (var currentProcess = Process.GetCurrentProcess())
        {
            _allowedProcessNames.Add(currentProcess.ProcessName);
        }

        if (extraAllowedProcessNames != null)
        {
            foreach (var name in extraAllowedProcessNames)
            {
                _allowedProcessNames.Add(name);
            }
        }

        // Initialize the hook
        _hookDelegate = OnForegroundChanged;
        _hookHandle = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _hookDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWinEvent(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        _targetChromeHwnd = IntPtr.Zero;
    }

    private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;

        GetWindowThreadProcessId(hwnd, out uint pid);
        string processName;

        try
        {
            // FIX: Use 'using' to prevent massive handle/memory leaks
            using (var process = Process.GetProcessById((int)pid))
            {
                processName = process.ProcessName;
            }
        }
        catch
        {
            return; // process already gone
        }

        if (_allowedProcessNames.Contains(processName)) return; // expected — ignore

        var titleBuf = new StringBuilder(256);
        GetWindowText(hwnd, titleBuf, titleBuf.Capacity);

        _log?.Invoke($"[FocusGuardian] Intrusion: {processName} — \"{titleBuf}\"");
        IntrusionDetected?.Invoke(this, new ForegroundIntrusionEventArgs
        {
            ProcessName = processName,
            WindowTitle = titleBuf.ToString()
        });

        // FIX: Shove focus back to the EXACT Selenium Chrome window
        if (_targetChromeHwnd != IntPtr.Zero)
        {
            SetForegroundWindow(_targetChromeHwnd);
        }
    }

    public bool IsClickTargetClear(int screenX, int screenY)
    {
        IntPtr hwndAtPoint = WindowFromPoint(new POINT { X = screenX, Y = screenY });
        IntPtr root = GetAncestor(hwndAtPoint, 2 /* GA_ROOT */);

        // FIX: Verify the window under the mouse is exactly our Selenium window
        return root == _targetChromeHwnd;
    }

    public void Dispose() => Stop();

    // --- P/Invoke Definitions ---

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
}