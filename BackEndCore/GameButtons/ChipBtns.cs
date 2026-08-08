using BackEndCore.Models;
using CSnakes.Runtime;
using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace BackEndCore.GameButtons;

public enum ChipType
{
    Chip_1 = 0,
    Chip_5 = 1,
    Chip_25 = 2,
    Chip_100 = 3,
    Chip_500 = 4
}

public class ChipBtns : UIButtonsBase
{    
    private readonly ICasinoSiteService _casinoSite;

    private readonly Dictionary<int, string> _boxNames = new()
    {
        [0] = "Chip 1",
        [1] = "Chip 5",
        [2] = "Chip 25",
        [3] = "Chip 100",
        [4] = "Chip 500",        
    };

    private readonly Dictionary<int, string> _expectedValues = new()
    {
        [0] = "1",
        [1] = "5",
        [2] = "25",
        [3] = "100",
        [4] = "500",
    };

    private readonly Dictionary<int, TargetBox> _boxes = new();

    private readonly string _clickEventName = "chip:selected";    

    public ChipBtns(IWebDriver? driver, IPythonEnvironment? python, ObservableCollection<TargetBox> boxes, ICasinoSiteService casinoSite) : base(driver, python)
    {
        for (int i = 0; i < 5; i++)
        {
            _boxes[i] = boxes.FirstOrDefault(b => b.Name == _boxNames[i]) ?? throw new ArgumentException($"Chip {_expectedValues[i]} box not found in the provided boxes.");
        }
        
        _casinoSite = casinoSite;
    }

    public async Task ClickButton(ChipType chip, CancellationToken token)
    {
        int chipIndex = (int)chip;

        if (!_boxes.TryGetValue(chipIndex, out TargetBox? box))
            throw new ArgumentException($"Invalid Chip index: {chipIndex}.");

        string expectedValue = _expectedValues[chipIndex];

        _casinoSite.LogInfo?.Invoke($"Clicking Chip {expectedValue}...");

        // 1. Fetch coordinates       
        int centerX = (int)(box.X + (box.Width / 2));
        int centerY = (int)(box.Y + (box.Height / 2));

        //2. Click button
        _python.BotGuide().MoveMouseTo(centerX, centerY);

        await Task.Delay(TimeSpan.FromSeconds(1), token);

        _python.BotGuide().ClickAt(centerX, centerY);

        bool isConfirmed = await WaitForGameEvent(_clickEventName, expectedValue, 2000);

        if (!isConfirmed)       
        {
            throw new Exception($"VERIFICATION FAILED: The game engine failed to verify the 'Chip {expectedValue} button click'.");
        }        
    }
}
