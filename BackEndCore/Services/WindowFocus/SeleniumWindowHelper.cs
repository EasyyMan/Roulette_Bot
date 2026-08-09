using OpenQA.Selenium;
using System.Runtime.InteropServices;
using System.Text;

namespace BackEndCore.Services.WindowFocus;

public static class SeleniumWindowHelper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    public static IntPtr GetNativeWindowHandle(this IWebDriver driver)
    {
        var js = (IJavaScriptExecutor)driver;
        string originalTitle = driver.Title;
        string uniqueTitle = Guid.NewGuid().ToString();

        js.ExecuteScript("document.title = arguments[0];", uniqueTitle);
        Thread.Sleep(50);

        IntPtr foundHwnd = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);

            if (sb.ToString().Contains(uniqueTitle))
            {
                foundHwnd = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        js.ExecuteScript("document.title = arguments[0];", originalTitle);

        return foundHwnd;
    }
}