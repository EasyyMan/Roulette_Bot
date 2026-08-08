using BackEndCore.Models;
using CSnakes.Runtime;
using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace BackEndCore.GameButtons;

public class BetTable : UIButtonsBase
{    
    private readonly Dictionary<int, string> _boxNamesAndExpectedValue = new()
    {
        [0] = "1-18",
        [1] = "Even",
        [2] = "Red",
        [3] = "Black",
        [4] = "Odd",
        [5] = "19-36",

        [6] = "1st 12",
        [7] = "2nd 12",
        [8] = "3rd 12",

        [9] = "top 2:1",
        [10] = "mid 2:1",
        [11] = "bottom 2:1"
    };

    private readonly Dictionary<int, TargetBox> _boxes = new();
    
    private readonly ICasinoSiteService _casinoSite;

    private readonly string _clickEventName = "boardSpot:Clicked";    

    public BetTable(IWebDriver? driver, IPythonEnvironment? python, ObservableCollection<TargetBox> boxes, ICasinoSiteService casinoSite) : base(driver, python)
    {
        for (int i = 0; i < 12; i++)
        {            
            _boxes[i] = boxes.FirstOrDefault(b => b.Name == _boxNamesAndExpectedValue[i]) ?? throw new ArgumentException($"Box for '{_boxNamesAndExpectedValue[i]}' not found in the provided boxes.");
        }
        
        _casinoSite = casinoSite;
    }

    public async Task ClickButton(int betKey, CancellationToken token)
    {
        if (!_boxes.TryGetValue(betKey, out TargetBox? box))
            throw new ArgumentException($"Invalid bet index: {betKey}.");

        string expectedValue = _boxNamesAndExpectedValue[betKey];
        
        _casinoSite.LogInfo?.Invoke($"Placing bet on {expectedValue}...");

        // 1. Fetch coordinates       
        int centerX = (int)(box.X + (box.Width / 2));
        int centerY = (int)(box.Y + (box.Height / 2));

        //2. Click button
        _python.BotGuide().MoveMouseTo(centerX, centerY);

        await Task.Delay(TimeSpan.FromSeconds(1), token);

        _python.BotGuide().ClickAt(centerX, centerY);

        // 3. Confirm the click
        bool isConfirmed = await WaitForGameEvent(_clickEventName, expectedValue, 2000);

        if (!isConfirmed)
        {
            throw new Exception($"VERIFICATION FAILED: The game engine failed to verify the click on '{expectedValue}' bet option.");
        }
    }
}
