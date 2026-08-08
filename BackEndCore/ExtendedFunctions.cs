using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BackEndCore;

public static class ExtendedFunctions
{

    public static void InjectEventSpy(this IWebDriver? driver)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(driver);

            string spyScript = @"
            if (!window.GameEventBuffer) {
                window.GameEventBuffer = [];
                
                for (const k in window) {
                    try {
                        const w = window[k];
                        if (!w || !w.bus) continue;
                        
                        const bus = w.bus;
                        if (typeof bus.emit === 'function' && !bus.__spy_patched) {
                            bus.__spy_patched = true;
                            bus.__orig_emit = bus.emit;
                            
                            bus.emit = function(name, payload) {
                                try {
                                    // 🛑 THE MASTER FILTER LIST 🛑
                                    const ignoredEvents = [
                                        'mousemove', 
                                        'mouseover', 
                                        'mouseout', 
                                        'mouse:screenOut', 
                                        'boardSpot:pointerOver', 
                                        'boardSpot:pointerOut', 
                                        'betHighlight:over', 
                                        'betHighlight:out',
                                        'mousedown',
                                        'mouseup',
                                        'pointerUp',
                                        'pointerDown',
                                        'sound:chipTap',
                                        'sound:chipMotion',
                                        'sound:chipPick',
                                        'sound:onHighlightAreaInfo',
                                        'sound:onOutcomeInfo',
                                        'sound:ballBounce2',
                                        'sound:ballRolling',
                                        'sound:spinClicked',
                                        'sound:chipToChipBank',
                                        'hideTip'
                                    ];

                                    // Only push to buffer if it's NOT in the ignored list
                                    if (!ignoredEvents.includes(name)) {
                                        window.GameEventBuffer.push({ 
                                            type: 'BusEvent',
                                            name: name, 
                                            payload: payload || {}, 
                                            source: k 
                                        });
                                    }
                                } catch (e) {}
                                return bus.__orig_emit.apply(this, arguments);
                            };
                        }
                    } catch (e) {}
                }

                document.addEventListener('click', function(e) {
                    try {
                        const t = e.target;
                        if (t && (t.tagName === 'BUTTON' || t.className.includes('btn'))) {
                            window.GameEventBuffer.push({ 
                                type: 'DOMEvent',
                                name: 'dom_button_click',
                                payload: t.title || t.className || 'unknown_button',
                                source: 'DOM' 
                            });
                        }
                    } catch (err) {}
                }, true);
            }
        ";

            ((IJavaScriptExecutor)driver).ExecuteScript(spyScript);

            Console.WriteLine("Event Spy injected successfully (with strict filtering)!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to inject spy: {ex.Message}");
        }
    }



    public static bool NotFoundWithIn40Seconds(this IWebDriver driver, Dictionary<int, string> xpath, ref int key, CancellationToken token)
    {
        // To wait 40 seconds checking every 2 seconds, we need 20 loops.
        int maxAttempts = 20;

        // Instantiate the wait object ONCE outside the loop
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                // Wait up to 2 seconds for the element to appear
                int n = key; // Capture the current value of key for the lambda
                wait.Until(d => d.FindElements(By.XPath(xpath[n])).Count > 0, token);

                // Element found! 
                // Note: I kept your original logic returning 'false' on success.
                return false;
            }
            catch (WebDriverTimeoutException)
            {
                // If it times out, toggle the key between 1 and 2 for the next loop iteration
                key = key == 1 ? 2 : 1;
            }
        }

        // If the loop completely finishes all 20 attempts, the 40 seconds are up.
        return true;
    }


    public static bool NotFoundWithIn_30_Seconds(this IWebDriver? driver, By? by, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(by);

        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            wait.Until(d => d.FindElements(by).Count > 0, token);

            return false;
        }
        catch (WebDriverTimeoutException)
        {

            return true;
        }

    }

    public static void Wait_30_SecondsForElement(this IWebDriver? driver, By? by)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(by);

        for (int i = 0; i < 6; i++)// 30 seconds
        {
            if (driver.FindElements(by).Count <= 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else { break; }
        }
    }

    public static void Wait_2_SecondsForElement(this IWebDriver? driver, By? by)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(by);

        for (int i = 0; i < 2; i++)// 2 seconds
        {
            if (driver.FindElements(by).Count <= 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            else { break; }
        }
    }


    public static bool ElementNotFound(this IWebDriver? driver, By? by)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(by);

        return driver.FindElements(by).Count <= 0;
    }
}