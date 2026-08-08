using CSnakes.Runtime;
using OpenQA.Selenium;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BackEndCore.GameButtons;

public class UIButtonsBase
{
    protected readonly IWebDriver _driver;
    protected readonly IPythonEnvironment _python;

    public UIButtonsBase(IWebDriver? driver, IPythonEnvironment? python)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _python = python ?? throw new ArgumentNullException(nameof(python));
    }


    protected async Task<bool> WaitForGameEvent(string targetEventName, string expectedValue, int timeoutMilliseconds = 2000)
    {
        DateTime startTime = DateTime.Now;

        // Loop until we hit the timeout limit
        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMilliseconds)
        {
            // 1. Pull the latest buffer
            string? jsonEvents = GetRecentGameEvents();

            if (!string.IsNullOrEmpty(jsonEvents) && jsonEvents != "[]")
            {
                try
                {
                    // 2. Parse the JSON array safely
                    using (JsonDocument doc = JsonDocument.Parse(jsonEvents))
                    {
                        foreach (JsonElement gameEvent in doc.RootElement.EnumerateArray())
                        {
                            // 3. Check if the event name matches
                            if (gameEvent.TryGetProperty("name", out JsonElement nameProp))
                            {
                                if (nameProp.GetString() == targetEventName)
                                {
                                    // If we don't care about the payload value, return true immediately!
                                    if (string.IsNullOrEmpty(expectedValue))
                                    {
                                        return true;
                                    }

                                    // Otherwise, let's validate the specific payloads
                                    if (gameEvent.TryGetProperty("payload", out JsonElement payload))
                                    {
                                        // CONDITION 1: chip:selected -> check payload.value
                                        if (targetEventName == "chip:selected")
                                        {
                                            if (payload.TryGetProperty("value", out JsonElement valProp))
                                            {
                                                // Using .ToString() ensures it matches whether the JSON value is an int (1) or a string ("1")
                                                if (valProp.ToString() == expectedValue)
                                                    return true;
                                            }
                                        }
                                        // CONDITION 2: boardSpot:Clicked -> check payload.boardSpot.view.config.text
                                        else if (targetEventName == "boardSpot:Clicked")
                                        {

                                            string? textValue = GetTextValue(payload);

                                            if (textValue == "2:1")
                                            {
                                                string? betTextValue = GetBetTextValue(payload);

                                                if (expectedValue == "top 2:1")
                                                {
                                                    if (betTextValue == "2:1 3~36")
                                                    {
                                                        return true;
                                                    }
                                                }
                                                else if (expectedValue == "mid 2:1")
                                                {
                                                    if (betTextValue == "2:1 2~35")
                                                    {
                                                        return true;
                                                    }
                                                }
                                                else if (expectedValue == "bottom 2:1")
                                                {
                                                    if (betTextValue == "2:1 1~34")
                                                    {
                                                        return true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (textValue == expectedValue)
                                                {
                                                    return true;
                                                }

                                                string? betTextValue = GetBetTextValue(payload);

                                                if (betTextValue == expectedValue)
                                                {
                                                    return true;
                                                }
                                            }                                                                                      
                                        }
                                        // FALLBACK: For other events where the payload is just a simple string (like State -> "IDLE")
                                        else if (payload.ValueKind == JsonValueKind.String)
                                        {
                                            if (payload.GetString() == expectedValue)
                                                return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"[Error] Parsing JSON buffer: {ex.Message}");
                }
            }

            // Wait 200ms before checking the buffer again (1000ms is too slow for automation)
            await Task.Delay(200);
        }

        return false; // The event never fired within the timeout
    }

    private static string? GetTextValue(JsonElement payload)
    {
        if (payload.TryGetProperty("boardSpot", out JsonElement boardSpot) &&
            boardSpot.TryGetProperty("view", out JsonElement view) &&
            view.TryGetProperty("config", out JsonElement config) &&
            config.TryGetProperty("text", out JsonElement textProp))
        {
            return textProp.GetString();
        }

        return null; // Or string.Empty, depending on your needs
    }

    private static string? GetBetTextValue(JsonElement payload)
    {
        if (payload.TryGetProperty("boardSpot", out JsonElement boardSpot) &&
            boardSpot.TryGetProperty("view", out JsonElement view) &&
            view.TryGetProperty("config", out JsonElement config) &&
            config.TryGetProperty("betText", out JsonElement betTextProp))
        {
            return betTextProp.GetString();
        }

        return null;
    }

    protected async Task<bool> WaitForGameEvent(string targetEventName, int timeoutMilliseconds = 2000)
    {
        DateTime startTime = DateTime.Now;

        // Loop until we hit the timeout limit
        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMilliseconds)
        {
            // 1. Pull the latest buffer
            string? jsonEvents = GetRecentGameEvents();

            if (!string.IsNullOrEmpty(jsonEvents) && jsonEvents != "[]")
            {
                try
                {
                    // 2. Parse the JSON array safely
                    using (JsonDocument doc = JsonDocument.Parse(jsonEvents))
                    {
                        foreach (JsonElement gameEvent in doc.RootElement.EnumerateArray())
                        {
                            // 3. Check if this specific event matches the one we are waiting for
                            if (gameEvent.TryGetProperty("name", out JsonElement nameProp))
                            {
                                if (nameProp.GetString() == targetEventName)
                                {
                                    return true; // Success! The event fired.
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"[Error] Parsing JSON buffer: {ex.Message}");
                }
            }

            // Wait 100ms before checking the buffer again
            await Task.Delay(1000);
        }

        return false; // The event never fired within the timeout
    }

    public string? GetRecentGameEvents()
    {
        try
        {            
            // This JS grabs the array, turns it to JSON, and empties it for the next check
            string fetchScript = @"
            const events = window.GameEventBuffer || [];
            const json = JSON.stringify(events);
            window.GameEventBuffer = []; // Clear the buffer
            return json;";

            var result = ((IJavaScriptExecutor)_driver).ExecuteScript(fetchScript);            

            string jsonResult = result?.ToString() ?? "[]";

            if (jsonResult != "[]")
            {
                // You can use System.Text.Json here to parse the result into a C# object!
                return jsonResult;
            }

            return null; // No new events
        }
        catch (Exception)
        {            
            return null;
        }
    }

    protected async Task TheClick(Point point, UIButtonsBase btn, CancellationToken token)
    {
        try
        {            
            await Task.Delay(TimeSpan.FromSeconds(3), token);            

            _python.BotGuide().MoveMouseTo(point.X, point.Y);

            await Task.Delay(TimeSpan.FromSeconds(1), token);

            _python.BotGuide().ClickAt(point.X, point.Y);            
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to click button '{btn.ToString()}': {ex.Message}");            
        }
    }
}
