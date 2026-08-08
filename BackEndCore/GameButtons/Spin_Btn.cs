using BackEndCore.Models;
using CSnakes.Runtime;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BackEndCore.GameButtons;

public class Spin_Btn : UIButtonsBase
{
    private readonly TargetBox _box;
    private readonly ICasinoSiteService _casinoSite;

    private readonly string _clickEventName = "State";
    private readonly string _expectedValue = "RESULTS_FINISHED";
    

    public Spin_Btn(IWebDriver? driver, IPythonEnvironment? python, ObservableCollection<TargetBox> boxes, ICasinoSiteService casinoSite) : base(driver, python)
    {
        var box = boxes.FirstOrDefault(b => b.Name == "Spin") ?? throw new ArgumentException("Spin box not found in the provided boxes.");
        _box = box;
        _casinoSite = casinoSite;
    }

    public async Task ClickButton(CancellationToken token)
    {
        _casinoSite.LogInfo?.Invoke("Clicking Spin button...");

        // 1. Fetch coordinates       
        int centerX = (int)(_box.X + (_box.Width / 2));
        int centerY = (int)(_box.Y + (_box.Height / 2));

        //2. Click button
        _python.BotGuide().MoveMouseTo(centerX, centerY);

        await Task.Delay(TimeSpan.FromSeconds(1), token);

        _python.BotGuide().ClickAt(centerX, centerY);

        // 3. Confirm the click
        bool isConfirmed = await WaitForGameEvent(_clickEventName, _expectedValue, 6000);

        if (!isConfirmed)       
        {
            throw new Exception("VERIFICATION FAILED: The game engine failed to verify the 'Spin button click'.");
        }
    }
}
