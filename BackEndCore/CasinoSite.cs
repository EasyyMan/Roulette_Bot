using BackEndCore.GameButtons;
using BackEndCore.Models;
using BackEndCore.Services;
using BackEndCore.Services.WindowFocus;
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BackEndCore;

public interface ICasinoSiteService
{
    Task StartAsync(IUIOverlayService overlay, SettingsConfigured settings, bool skipEditingYellowBoxes, CancellationToken token);

    void Stop();    
    

    // Callbacks that consumers can assign to receive log messages from the service.
    Action<string>? LogInfo { get; set; }
    Action<string>? LogError { get; set; }
    Action<string>? LogGoodNews { get; set; }
    Action<string, string>? LogCustom { get; set; }
    
}

public class CasinoSite : ICasinoSiteService
{
    // Implement callbacks from the interface so callers (e.g. ViewModels) can assign delegates
    public Action<string>? LogInfo { get; set; }
    public Action<string>? LogError { get; set; }
    public Action<string>? LogGoodNews { get; set; }
    public Action<string, string>? LogCustom { get; set; }

    private readonly string _url = "https://www.cafecasino.lv/casino/table-games/roulette/european-roulette#quickplay";
    
    private readonly IPythonEnvironment? _python;

    private IWebDriver? _driver;

    private IUIOverlayService? _overlayWindow;

    private SettingsConfigured? _settingsConfig;

    private double _currentBalance = 0;

    private double _highestBalance = double.MinValue;

    private DateTime _sessionStartTime = DateTime.Now;

    private double _initialBalance;    

    private int _xpathKey = 1;

    private RemoveAnimationBtn? _removeAnimationBtn;
    private ChipBtns? _chips;
    private BetTable? _betTable;
    private Spin_Btn? _spin_Btn;
    private Rebet_Btn? _rebet_Btn;
    private X2_Btn? _x2_Btn;

    private TwelveSectionService _twelveSectionService = new();

    private readonly Random _random = new Random();

    private IChromeFocusGuardianService _focusGuardian = new ChromeFocusGuardianService();

    private int _lastBetKey = -1;
    private List<int> _1st2nd3rd_12s_lastBetKeys = new List<int>();
    private List<int> _topToBottom_21_lastBetKeys = new List<int>();



    private readonly Dictionary<int, string> _fullScreenButtonXpath = new Dictionary<int, string>
    {
        [1] = "/html/body/div[2]/div/div/div[3]/div[2]/div[1]/button[1]",
        [2] = "/html/body/div[3]/div/div/div[3]/div[2]/div[1]/button[1]"
    };

    private readonly Dictionary<int, string> _exitFullScreenButtonXpath = new Dictionary<int, string>
    {
        [1] = "/html/body/div[2]/div/div/div[3]/div[1]/div/div[1]/div/div/div[1]/button",
        [2] = "/html/body/div[3]/div/div/div[3]/div[1]/div/div[1]/div/div/div[1]/button"
    };

    private readonly Dictionary<int, string> _realOrPracticePlayBtnXpath = new Dictionary<int, string>
    {
        [1] = "/html/body/div[2]/div/div/div[3]/div[2]/div[2]/a",
        [2] = "/html/body/div[3]/div/div/div[3]/div[2]/div[2]/a"
    };

    public CasinoSite()
    {
        // Initialize Python environment

        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());

        var home = GetHomeDirectory();
        //var home = Path.Join(Environment.CurrentDirectory, "python");

        builder.Services
            .WithPython()
            .WithHome(home)
            .WithVirtualEnvironment(Path.Join(home, "env"))
            .WithPipInstaller()
            .FromRedistributable();

        var app = builder.Build();
        _python = app.Services.GetRequiredService<IPythonEnvironment>();

        ArgumentNullException.ThrowIfNull(_python);        
    }

    #region HomeDirectoryConfiguration
    public static string GetHomeDirectory()
    {
        const string fileDependency1 = "bot_guide.py";
        const string fileDependency2 = "requirements.txt";

        // Use BaseDirectory — most reliable location in ClickOnce deployments
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var home = Path.Join(baseDir, "python");

        // Always make sure the python folder exists
        Directory.CreateDirectory(home);

        // Check if both required files are already in the home folder
        bool hasFile1 = File.Exists(Path.Combine(home, fileDependency1));
        bool hasFile2 = File.Exists(Path.Combine(home, fileDependency2));

        // If both files are already present → we're good
        if (hasFile1 && hasFile2)
            return home;

        // At least one file is missing → search for them in the deployment
        string? source1 = FindFile(fileDependency1, baseDir);
        string? source2 = FindFile(fileDependency2, baseDir);

        // Both files MUST be found somewhere in the deployment
        if (string.IsNullOrEmpty(source1) || string.IsNullOrEmpty(source2))
        {
            throw new FileNotFoundException(
                $"Missing required Python dependency files.\n\n" +
                $"Could not locate both '{fileDependency1}' and '{fileDependency2}' " +
                $"in the application deployment folder.");
        }

        // Copy the missing/newer files into our python folder
        CopyIfNewer(source1, Path.Combine(home, fileDependency1));
        CopyIfNewer(source2, Path.Combine(home, fileDependency2));

        return home;
    }

    /// <summary>
    /// Recursively searches for a file starting from the given directory.
    /// Returns the full path or null if not found.
    /// </summary>
    private static string? FindFile(string fileName, string startDirectory)
    {
        // Check directly in start directory first (fastest path)
        string directPath = Path.Combine(startDirectory, fileName);
        if (File.Exists(directPath))
            return directPath;

        // Then search all subdirectories (covers ClickOnce cache scattering)
        try
        {
            foreach (string file in Directory.EnumerateFiles(startDirectory, fileName, SearchOption.AllDirectories))
            {
                return file;
            }
        }
        catch
        {
            // Ignore permission/access issues in deep cache folders
        }

        return null;
    }

    /// <summary>
    /// Copies the file only if it doesn't exist or is newer than the destination.
    /// </summary>
    private static void CopyIfNewer(string sourcePath, string destinationPath)
    {
        if (!File.Exists(destinationPath) ||
            File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath))
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    #endregion

    private void OnIntrusionDetected(object sender, ForegroundIntrusionEventArgs e)
    {
        LogInfo?.Invoke($"⚠ Foreign window took focus: {e.ProcessName} — \"{e.WindowTitle}\"");
    }

    public async Task StartAsync(IUIOverlayService overlay, SettingsConfigured settings, bool skipEditingYellowBoxes, CancellationToken token)
    {        
        _settingsConfig = settings;
        
        _sessionStartTime = DateTime.Now;

        _overlayWindow = overlay;

        await OpenSiteAsync(token); // waits for 20 seconds for the site

        await CloseCookieWindowAsync(token);

        // after ChromeDriver is launched:
        _focusGuardian.Start();
        _focusGuardian.IntrusionDetected += OnIntrusionDetected!;        


        Login();                

        await Task.Delay(TimeSpan.FromSeconds(4), token);
              
        while (!token.IsCancellationRequested) 
        {                                     
            if (_settingsConfig.IsRealPlay)
            {
                if (IsPracticePlayCurrentPlayMode(token)) await ClickRealPlayButtonAsync(token);
                token.ThrowIfCancellationRequested();
            }
            else
            {
                if (IsRealPlayCurrentPlayMode(token)) await ClickPracticePlayButtonAsync(token);
                token.ThrowIfCancellationRequested();
            }

            await PressFullScreenButtonAsync(token);
            token.ThrowIfCancellationRequested();

            await EnterGameFrameAsync(skipEditingYellowBoxes, token);//========================= Switch To Game Frame ====================================

            await Task.Delay(TimeSpan.FromSeconds(2), token);// wait 2 seconds.

            await _removeAnimationBtn!.ClickButton(token);
            token.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(1), token);// wait for the game to process a bit.            

            InitializeCurrentBalance();
            token.ThrowIfCancellationRequested();

            //await Task.Delay(TimeSpan.FromSeconds(2), token);// wait 2 seconds.
            
            await EnterGameCycleAsync(token);
            token.ThrowIfCancellationRequested();

            _driver!.SwitchTo().DefaultContent();//================================ Exit Game Frame ====================================

            await PressExitFullScreenButtonAsync(token);
            token.ThrowIfCancellationRequested();

            // Pass the token down to methods that need to sleep
            await StartOperatingAgainAfterAsync(token);
            token.ThrowIfCancellationRequested();

            LogInfo?.Invoke("Maximizing browser window...");
            _driver!.Manage().Window.Maximize();

            await Task.Delay(TimeSpan.FromSeconds(5), token);// wait
            token.ThrowIfCancellationRequested();

            LogInfo?.Invoke("Maximized browser window.");

            LogInfo?.Invoke($"Refreshing site.");
            _driver!.Navigate().Refresh();

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            var iframe = wait.Until(d =>
            {
                return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")!.Equals("complete");
            }, token
            );

            token.ThrowIfCancellationRequested();
        }
    }

    private async Task EnterGameFrameAsync(bool skipEditingYellowBoxes, CancellationToken token)
    {
        if (CheckPandoraObjectExists_AndSwitchToGameFrame(token))// waits upto 20 seconds
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
           
            if (skipEditingYellowBoxes)
            {
                _overlayWindow!.LoadBoxesConfiguration();

                var boxes2 = _overlayWindow.GetTargetBoxes();

                if (boxes2.Count == 0)
                {
                    throw new ArgumentException("No target boxes found in configuration.");
                }
            }
            else if (!_overlayWindow!.IsLocked())
            {
                _overlayWindow!.ShowOverlayWindow();

                while (!_overlayWindow.IsLocked())
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                }                
            }

            var boxes = _overlayWindow!.GetTargetBoxes();

            if (boxes.Count == 0)
            {
                throw new ArgumentException("No target boxes found in configuration.");
            }

            _removeAnimationBtn = new RemoveAnimationBtn(_driver, _python, boxes, this);

            _chips = new ChipBtns(_driver, _python, boxes, this);

            _betTable = new BetTable(_driver, _python, boxes, this);

            _spin_Btn = new Spin_Btn(_driver, _python, boxes, this);

            _rebet_Btn = new Rebet_Btn(_driver, _python, boxes, this);

            _x2_Btn = new X2_Btn(_driver, _python, boxes, this);

            _driver.InjectEventSpy();
        }
        else
        {
            throw new ArgumentException("Failed to locate game object.");
        }
    }

    private async Task StartOperatingAgainAfterAsync(CancellationToken token)
    {
        _driver!.Manage().Window.Minimize();
        LogInfo?.Invoke("Minimized browser window.");

        int result = 0;

        if (_settingsConfig!.ThenStartOperatingAgainAfter.IsRandomInterval)
        {
            int rawBtwn1 = _settingsConfig.ThenStartOperatingAgainAfter.Btwn1;
            int rawBtwn2 = _settingsConfig.ThenStartOperatingAgainAfter.Btwn2 + 1;

            // FIXED: Assign to new variables to prevent overwriting
            int min = Math.Min(rawBtwn1, rawBtwn2);
            int max = Math.Max(rawBtwn1, rawBtwn2);

            // Best practice: use Random.Shared (if .NET 6+)
            result = Random.Shared.Next(min, max);
        }
        else
        {
            result = _settingsConfig.ThenStartOperatingAgainAfter.FixedEvery;
        }

        if (_settingsConfig.ThenStartOperatingAgainAfter.IsInMinutes)
        {
            LogInfo?.Invoke($"Bot will sleep for {result} minute{(result == 1 ? "" : "s")} and then resume.");
            await Task.Delay(TimeSpan.FromMinutes(result), token); // Cancellable
        }
        else
        {
            LogInfo?.Invoke($"Bot will sleep for {result} hour{(result == 1 ? "" : "s")} and then resume.");
            await Task.Delay(TimeSpan.FromHours(result), token); // Cancellable
        }
    }

    public void Stop()
    {
        _focusGuardian.Stop();
        _focusGuardian.IntrusionDetected -= OnIntrusionDetected!;        

        LogInfo?.Invoke($"Closing site...");
        _driver?.Quit();
        _driver?.Dispose();
        _driver = null;
        LogInfo?.Invoke("Site closed successfully.");
    }

    private async Task OpenSiteAsync(CancellationToken token)
    {
        LogInfo?.Invoke($"Opening site...");

        try
        {
            _driver = OpenSite(_url, token);

            await Task.Delay(TimeSpan.FromSeconds(20), token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArgumentException("Failed to open site.");            
        }        

        LogGoodNews?.Invoke("Site opened successfully.");
    }


    public bool CheckPandoraObjectExists_AndSwitchToGameFrame(CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(_driver);
        try
        {
            // 1. Switch to the game-iframe to access window.Pandora
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

            var iframe = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.ClassName("game-iframe")));

            _driver.SwitchTo().Frame(iframe);

            var wait2 = new WebDriverWait(_driver, TimeSpan.FromSeconds(40));

            wait2.Until(d =>
            {
                // The '!!' converts the object into a strict true/false boolean in JavaScript
                string js = "return !!(window.Pandora || window.o || window.gameProxy || window.game);";

                var result = ((IJavaScriptExecutor)d).ExecuteScript(js);

                // Safely convert the returned object to a C# boolean
                return Convert.ToBoolean(result);
            }, token);

            return true;
        }
        catch (WebDriverTimeoutException)
        {            
            return false;
        }
    }


    private void Login()
    {
        if (!_settingsConfig!.IsRealPlay) return;

        LogInfo?.Invoke("Logging in...");

        ArgumentNullException.ThrowIfNull(_driver);
        
        Thread.Sleep(TimeSpan.FromSeconds(5));        

        WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        var xpath = @"/html/body/div[1]/div/header/div[3]/div[1]/div/a";
        var by = By.XPath(xpath);
        var loginBtn = wait.Until(d => d.FindElement(by));
        loginBtn.Click();

        Thread.Sleep(TimeSpan.FromSeconds(5));

        xpath = "//input[@id='email']";
        by = By.XPath(xpath);
        var emailInput = wait.Until(d => d.FindElement(by));
        emailInput.SendKeys(_settingsConfig.Email);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        xpath = "//input[@id='login-password']";
        by = By.XPath(xpath);
        var passwordInput = wait.Until(d => d.FindElement(by));
        passwordInput.SendKeys(_settingsConfig.Password);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        xpath = "//button[@id='login-submit']";
        by = By.XPath(xpath);
        loginBtn = wait.Until(d => d.FindElement(by));
        loginBtn.Click();

        //WAIT

        //Check if login form is still present
        xpath = @"/html/body/bx-site/bx-root-component/div[1]/bx-homepage/bx-homepage-content/bx-homepage-overlay/bx-overlay/div/div/bx-login-overlay";
        by = By.XPath(xpath);

        for (int i = 0; i < 6; i++)// 30 seconds
        {
            if (_driver.FindElements(by).Count > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else { break; }
        }

        if (_driver.FindElements(by).Count > 0)
        {
            throw new ArgumentException("Login failed: Login form still present after waiting.");
        }

        Thread.Sleep(TimeSpan.FromSeconds(5));

        LogGoodNews?.Invoke("Logged in successfully.");
    }

    private async Task CloseCookieWindowAsync(CancellationToken token)
    {
        var xpath = "//*[@id=\"coi-banner-wrapper\"]";
        var by = By.XPath(xpath);

        _driver!.Wait_2_SecondsForElement(by);

        if (_driver.ElementNotFound(by)) return;

        // x button
        xpath = "//*[@id=\"coiPage-1\"]/div[2]/div/button[3]";
        by = By.XPath(xpath);

        _driver!.Wait_2_SecondsForElement(by);

        if (_driver.ElementNotFound(by)) return;

        var btn = _driver!.FindElement(by);
        btn.Click();

        await Task.Delay(TimeSpan.FromSeconds(2), token);        
    }

    private bool IsPracticePlayCurrentPlayMode(CancellationToken token)
    {
        if (_driver!.NotFoundWithIn40Seconds(_realOrPracticePlayBtnXpath, ref _xpathKey, token))
            throw new ArgumentException("Practice/Real play button not found.");

        var btn = _driver!.FindElement(By.XPath(_realOrPracticePlayBtnXpath[_xpathKey]));

        string innertext = btn.Text;

        if(string.IsNullOrEmpty(innertext)) throw new ArgumentException("Practice/Real play button text is empty.");

        return innertext.Contains("real", StringComparison.CurrentCultureIgnoreCase);
    }

    private async Task ClickPracticePlayButtonAsync(CancellationToken token)
    {
        LogInfo?.Invoke("Clicking Practice Play button...");

        if (_driver!.NotFoundWithIn40Seconds(_realOrPracticePlayBtnXpath, ref _xpathKey, token))
            throw new ArgumentException("Practice play button not found.");

        var btn = _driver!.FindElement(By.XPath(_realOrPracticePlayBtnXpath[_xpathKey]));

        string innertext = btn.Text;

        if (string.IsNullOrEmpty(innertext)) throw new ArgumentException("Practice/Real play button text is empty.");

        var result = innertext.Contains("practice", StringComparison.CurrentCultureIgnoreCase);
        if (result)
        {
            btn.Click();

            await Task.Delay(TimeSpan.FromSeconds(7), token);// wait for the game to process a bit.

            LogGoodNews?.Invoke($"Practice Play button clicked successfully.");
        }
        else throw new ArgumentException("Practice play button not found.");
    }

    private bool IsRealPlayCurrentPlayMode(CancellationToken token)
    {                
        if (_driver!.NotFoundWithIn40Seconds(_realOrPracticePlayBtnXpath, ref _xpathKey, token))
            throw new ArgumentException("Practice/Real play button not found.");

        var btn = _driver!.FindElement(By.XPath(_realOrPracticePlayBtnXpath[_xpathKey]));                

        string innertext = btn.Text;

        if (string.IsNullOrEmpty(innertext)) throw new ArgumentException("Practice/Real play button text is empty.");

        return innertext.Contains("practice", StringComparison.CurrentCultureIgnoreCase);
    }

    private async Task ClickRealPlayButtonAsync(CancellationToken token)
    {
        LogInfo?.Invoke("Clicking Real Play button...");

        if (_driver!.NotFoundWithIn40Seconds(_realOrPracticePlayBtnXpath, ref _xpathKey, token))
            throw new ArgumentException("Real play button not found.");

        var btn = _driver!.FindElement(By.XPath(_realOrPracticePlayBtnXpath[_xpathKey]));

        string innertext = btn.Text;

        if (string.IsNullOrEmpty(innertext)) throw new ArgumentException("Practice/Real play button text is empty.");

        var result = innertext.Contains("real", StringComparison.CurrentCultureIgnoreCase);
        if (result)
        {
            btn.Click();

            await Task.Delay(TimeSpan.FromSeconds(7), token);// wait for the game to process a bit.

            LogGoodNews?.Invoke($"Real Play button clicked successfully.");
        }
        else throw new ArgumentException("Real play button not found.");
    }

    private async Task PressFullScreenButtonAsync(CancellationToken token)
    {
        LogInfo?.Invoke($"Pressing full screen button...");

        ArgumentNullException.ThrowIfNull(_driver);

        if (_driver.NotFoundWithIn40Seconds(_fullScreenButtonXpath, ref _xpathKey, token))
            throw new ArgumentException("Full screen button not found.");

        var fullScreenBtn = _driver.FindElement(By.XPath(_fullScreenButtonXpath[_xpathKey]));
        fullScreenBtn.Click();

        await Task.Delay(TimeSpan.FromSeconds(4), token);

        LogGoodNews?.Invoke($"Full screen button pressed successfully.");
    }

    private async Task PressExitFullScreenButtonAsync(CancellationToken token)
    {
        LogInfo?.Invoke($"Pressing exit full screen button...");

        ArgumentNullException.ThrowIfNull(_driver);

        if (_driver.NotFoundWithIn40Seconds(_exitFullScreenButtonXpath, ref _xpathKey, token))
            throw new ArgumentException("Exit full screen button not found.");

        var exitFullScreenBtn = _driver.FindElement(By.XPath(_exitFullScreenButtonXpath[_xpathKey]));
        exitFullScreenBtn.Click();

        await Task.Delay(TimeSpan.FromSeconds(3), token);

        LogGoodNews?.Invoke($"Exit full screen button pressed successfully.");
    }    

    private static IWebDriver? OpenSite(string url, CancellationToken token)
    {
        var chromeDriverService = ChromeDriverService.CreateDefaultService();
        chromeDriverService.HideCommandPromptWindow = true;

        var options = new ChromeOptions();
        IWebDriver driver = new ChromeDriver(chromeDriverService, options);

        driver.Manage().Window.Maximize();

        driver.Navigate().GoToUrl(url);
        
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        var iframe = wait.Until(d =>
        {
            return ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")!.Equals("complete");
        }, token
        );

        return driver;
    }

    private async Task EnterGameCycleAsync(CancellationToken token)
    {
        if (_settingsConfig!.IsBetOnOption_1)
        {
            await PlacingOnBetOn_Option_1(token);
        }
        else
        {
            await PlacingOnBetOn_Option_2(token);
        }
    }

    private async Task PlacingOnBetOn_Option_1(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            double balance_1 = AccountBalance();

            // step 1 - click chip 1
            await _chips!.ClickButton(ChipType.Chip_1, token);

            //await Task.Delay(TimeSpan.FromSeconds(1), token);
            token.ThrowIfCancellationRequested();

            // step 2 - place bet            
            int selectedKey = _random.Next(6);
            await _betTable!.ClickButton(selectedKey, token);
            _lastBetKey = selectedKey;

            //await Task.Delay(TimeSpan.FromSeconds(1), token);
            token.ThrowIfCancellationRequested();

            // step 3 - click bet button
            await _spin_Btn!.ClickButton(token);

            //await Task.Delay(TimeSpan.FromSeconds(1), token);
            token.ThrowIfCancellationRequested();

            // step 4 - Check if bet won and update the balance

            bool won = CheckIfWonAndUpdateBalance();
            if (won)
            {
                //can terminate cycle and wait for next session.
                if (ValidateStopOperatingAfter()) break;
            }
            else
            {
                await RecoveryCycleAsync(balance_1, token);
                //can terminate cycle and wait for next session.
                if (ValidateStopOperatingAfter()) break;
            }
        }
    }


    private async Task PlacingOnBetOn_Option_2(CancellationToken token)
    {
        if (_settingsConfig!.BetOn_12sectionMode == BetOn12section.BetOn_Both_Sets)
        {
            await BetOnBoth_12Sections(token);
        }
        else if(_settingsConfig.BetOn_12sectionMode == BetOn12section.BetOn_1_Set_At_A_Time)
        {
            await BetOnOne_12Sections(token);
        }
        else
        {
            await BetOn_Both_Sets_1_Set_With_1_Bet_12Sections(token);
        }
    }

    private async Task BetOn_Both_Sets_1_Set_With_1_Bet_12Sections(CancellationToken token)
    {
        double chipAmount = 1;

        while (!token.IsCancellationRequested)
        {
            double balance = AccountBalance();

            if (chipAmount > _settingsConfig!.ResetMarkAmount)
            {
                chipAmount = 1;                
            }

            SetRandomBetOn12SectionsAsync(chipAmount);

            await PlaceBetForSectionAsync(chipAmount, token);

            token.ThrowIfCancellationRequested();

            // step 3 - click bet button
            await _spin_Btn!.ClickButton(token);

            token.ThrowIfCancellationRequested();

            double balance_result = AccountBalance();

            bool won = balance_result > balance; // wins 2 bets of the 3 betting locations.
            bool lost = balance_result < balance; // losses all 3 bets of the 3 betting locations.

            if (won)
            {
                chipAmount = 1;
                if (ValidateStopOperatingAfter()) break;
            }
            else if (lost)
            {
                chipAmount *= 2;
            }
        }
    }

    private async Task BetOnOne_12Sections(CancellationToken token)
    {
        double chipAmount = 1;

        while (!token.IsCancellationRequested)
        {
            if (chipAmount > _settingsConfig!.ResetMarkAmount)
            {
                chipAmount = 1;
                _twelveSectionService.ResetLossCount();
            }

            SetRandomBetOn12SectionsAsync(chipAmount);

            await PlaceBetForSectionAsync(chipAmount, token);

            token.ThrowIfCancellationRequested();

            // step 3 - click bet button
            await _spin_Btn!.ClickButton(token);

            token.ThrowIfCancellationRequested();

            // step 4 - Check if bet won and update the balance

            bool won = CheckIfWonAndUpdateBalance();
            if (won)
            {
                chipAmount = 1;
                _twelveSectionService.ResetLossCount();

                //can terminate cycle and wait for next session.
                if (ValidateStopOperatingAfter()) break;
            }
            else
            {
                _twelveSectionService.AddLoss();
                chipAmount = _twelveSectionService.GetNextChipAmount(chipAmount, _settingsConfig!.ChipAmountCalc_12sectionMode);
            }
        }
    }

    private void SetRandomBetOn12SectionsAsync(double chipAmount)
    {        
        switch (_settingsConfig!.RandomBet_12sectionMode)
        {
            case RandomBet_12section.BetOn_SameBets_EveryTime:
                
                break;
            case RandomBet_12section.BetOn_RandomBets_EveryTime:

                Initialize_1st2nd3rd_12s_lastBetKeys();
                Initialize_topToBottom_21_lastBetKeys();

                break;
            case RandomBet_12section.BetOn_RandomBets_DuringWins:

                if (chipAmount == 1)
                {
                    Initialize_1st2nd3rd_12s_lastBetKeys();
                    Initialize_topToBottom_21_lastBetKeys();
                }

                break;
            default:
                break;
        }

        if (_1st2nd3rd_12s_lastBetKeys.Count == 0 && _topToBottom_21_lastBetKeys.Count == 0)
        {
            Initialize_1st2nd3rd_12s_lastBetKeys();
            Initialize_topToBottom_21_lastBetKeys();
        }
    }

    private async Task PlaceBetForSectionAsync(double chipAmount, CancellationToken token)
    {
        if (_twelveSectionService.GetNextSectionIndex() == 1)
        {
            await PlaceDoubleBet(_1st2nd3rd_12s_lastBetKeys, chipAmount, token);

            // opposite third bet for the 'BetOn_Both_Sets_1_Set_With_1_Bet' Strategy
            await PlaceThirdBet(_topToBottom_21_lastBetKeys, chipAmount, token);
        }            
        else
        {
            await PlaceDoubleBet(_topToBottom_21_lastBetKeys, chipAmount, token);

            // opposite third bet for the 'BetOn_Both_Sets_1_Set_With_1_Bet' Strategy
            await PlaceThirdBet(_1st2nd3rd_12s_lastBetKeys, chipAmount, token);
        }            
    }


    private async Task BetOnBoth_12Sections(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // step 1 - click chip 1
            //await _chips!.ClickButton(ChipType.Chip_1, token);

            token.ThrowIfCancellationRequested();

            double chipAmount = GetChipAmount();

            if (chipAmount > _settingsConfig!.ResetMarkAmount)
            {
                _highestBalance = AccountBalance();
                chipAmount = GetChipAmount();
            }

            
            if (_1st2nd3rd_12s_lastBetKeys.Count == 0 && _topToBottom_21_lastBetKeys.Count == 0)
            {
                Initialize_1st2nd3rd_12s_lastBetKeys();
                Initialize_topToBottom_21_lastBetKeys();
            }

            // step 2 - place bet            
            await PlaceDoubleBet(_1st2nd3rd_12s_lastBetKeys, chipAmount, token);
            await PlaceDoubleBet(_topToBottom_21_lastBetKeys, chipAmount, token);

            token.ThrowIfCancellationRequested();

            // step 3 - click bet button
            await _spin_Btn!.ClickButton(token);


            token.ThrowIfCancellationRequested();

            // step 4 - Check if bet won and update the balance

            bool won = CheckIfWonAndUpdateBalance();
            if (won)
            {
                Initialize_1st2nd3rd_12s_lastBetKeys();
                Initialize_topToBottom_21_lastBetKeys();

                //can terminate cycle and wait for next session.
                if (ValidateStopOperatingAfter()) break;
            }
            else
            {
                if (_settingsConfig!.BetRandomBetsEveryTime)
                {
                    Initialize_1st2nd3rd_12s_lastBetKeys();
                    Initialize_topToBottom_21_lastBetKeys();
                }
            }
        }
    }

    private double GetChipAmount()
    {
        double currentBalance = AccountBalance();

        double chipAmount = 1;

        double num = _settingsConfig!.IsChipAmountCalc_Option_1 ? (_highestBalance - currentBalance) / 2 :
                                                                  ((_highestBalance - currentBalance) + 1) / 2;
        num = Math.Round(num);
        
        chipAmount = Math.Max(chipAmount, num);

        return chipAmount;
    }

    private async Task PlaceDoubleBet(List<int> numbers, double chipAmount, CancellationToken token)
    {
        foreach (var num in numbers)
        {
            int selectedKey = num;
            await PlaceAutomatedBetAsync(chipAmount, selectedKey, token);            
        }
    }

    private async Task PlaceThirdBet(List<int> numbers, double chipAmount, CancellationToken token)
    {
        if (_settingsConfig!.BetOn_12sectionMode == BetOn12section.BetOn_Both_Sets_1_Set_With_1_Bet)
        {
            foreach (var num in numbers)
            {
                int selectedKey = num;
                await PlaceAutomatedBetAsync(chipAmount, selectedKey, token);
                break; // Only place the bet on the first number in the list
            }
        }        
    }

    private void Initialize_1st2nd3rd_12s_lastBetKeys()
    {
        if(_1st2nd3rd_12s_lastBetKeys.Count > 0) _1st2nd3rd_12s_lastBetKeys.Clear();

        int[] numbers = { 6, 7, 8 };
        int randomNumber = numbers[_random.Next(numbers.Length)];

        _1st2nd3rd_12s_lastBetKeys.Add(randomNumber);

        while (randomNumber == _1st2nd3rd_12s_lastBetKeys[0])
        {
            randomNumber = numbers[_random.Next(numbers.Length)];
        }

        _1st2nd3rd_12s_lastBetKeys.Add(randomNumber);
    }

    private void Initialize_topToBottom_21_lastBetKeys()
    {
        if (_topToBottom_21_lastBetKeys.Count > 0) _topToBottom_21_lastBetKeys.Clear();

        int[] numbers = { 9, 10, 11 };
        int randomNumber = numbers[_random.Next(numbers.Length)];

        _topToBottom_21_lastBetKeys.Add(randomNumber);

        while (randomNumber == _topToBottom_21_lastBetKeys[0])
        {
            randomNumber = numbers[_random.Next(numbers.Length)];
        }

        _topToBottom_21_lastBetKeys.Add(randomNumber);
    }

    private bool ValidateStopOperatingAfter()
    {
        if (_settingsConfig!.StopOperatingAfter.IsDurationOfTime)
        {
            var timeElapsed = DateTime.Now - _sessionStartTime;

            if (_settingsConfig.StopOperatingAfter.IsInMinutes)
            {
                return timeElapsed.TotalMinutes >= _settingsConfig.StopOperatingAfter.Duration;
            }
            else
            {
                return timeElapsed.TotalHours >= _settingsConfig.StopOperatingAfter.Duration;
            }          
        }
        else
        {
            double diff = _currentBalance - _initialBalance;
            if (diff > 0)
            {
                return diff >= _settingsConfig.StopOperatingAfter.Dollar;
            }
        }

        return false;
    }

    private async Task RecoveryCycleAsync(double balance_1, CancellationToken token)
    {
        double balance_2 = AccountBalance();
        double totalLoss = Math.Abs(balance_1 - balance_2);
        double stake = totalLoss * 2;

        while (!token.IsCancellationRequested)
        {
            if (stake > _settingsConfig!.ResetMarkAmount)
            {
                break;
            }

            token.ThrowIfCancellationRequested();

            int selectedBetKey = GetBetKey();
            _lastBetKey = selectedBetKey;

            await PlaceAutomatedBetAsync(stake, selectedBetKey, token);

            await _spin_Btn!.ClickButton(token);                          

            // Check if bet won and update the balance
            bool won = CheckIfWonAndUpdateBalance();
            if (won)
            {
                //can terminate cycle and wait for next session.
                break;
            }
            else
            {               
                stake *= 2;
            }

            token.ThrowIfCancellationRequested();
        }
    }

    private int GetBetKey()
    {
        if (_settingsConfig!.BetRandomBetsEveryTime)
        {
            int selectedBetKey = _random.Next(6);

            while (selectedBetKey == _lastBetKey)
            {
                selectedBetKey = _random.Next(6);
            }

            return selectedBetKey;
        }
        else
        {
            return _lastBetKey;
        }
    }

    public async Task PlaceAutomatedBetAsync(double requestedBetAmount, int selectedBetKey, CancellationToken token)
    {
        // 1. Verify and adjust the bet amount based on the current account balance
        double currentBalance = AccountBalance();
        double finalBetAmount = requestedBetAmount > currentBalance ? currentBalance : requestedBetAmount;
       
        // Convert to an integer since chip values are whole numbers
        int targetStake = (int)Math.Floor(finalBetAmount);

        LogInfo?.Invoke($"Placing ${targetStake} Bet...");

        // Guard clause: Ensure we have enough to place at least a $1 bet
        if (targetStake < 1)
        {
            throw new ArgumentException("Insufficient balance to place a minimum bet of 1.");            
        }       

        // 3. Place the bet using the largest available chips first (Greedy Approach)
        int remainingStake = targetStake;

        // Ordered array from highest to lowest denomination
        var availableChips = new (int Value, ChipType Type)[]
        {
            (500, ChipType.Chip_500),
            (100, ChipType.Chip_100),
            (25,  ChipType.Chip_25),
            (5,   ChipType.Chip_5),
            (1,   ChipType.Chip_1)
        };

        foreach (var chip in availableChips)
        {
            // Check if the remaining stake is large enough to use this chip
            if (remainingStake >= chip.Value)
            {
                // Determine how many times we need to click the bet table with this chip
                int requiredClicks = remainingStake / chip.Value;

                // Step A: Select the chip type
                await _chips!.ClickButton(chip.Type, token);

                //await Task.Delay(TimeSpan.FromSeconds(1), token);
                token.ThrowIfCancellationRequested();

                // Step B: Click the bet table the calculated number of times
                for (int i = 0; i < requiredClicks; i++)
                {
                    await _betTable!.ClickButton(selectedBetKey, token);

                    //await Task.Delay(TimeSpan.FromSeconds(1), token);
                    token.ThrowIfCancellationRequested();
                }

                // Step C: Update the remaining amount left to place
                remainingStake %= chip.Value;
            }
        }
    }


    private void InitializeCurrentBalance()
    {
        string? balance = GetCurrentBalance();

        if (balance is not null)
        {
            if (double.TryParse(balance, out double balanceValue))
            {
                if(balanceValue < 2)
                {
                    throw new ArgumentException($"Balance value is too low: {balanceValue}");
                }

                _currentBalance = balanceValue;
                _initialBalance = balanceValue;
                _highestBalance = Math.Max(balanceValue, _highestBalance);

                LogGoodNews?.Invoke($"Current balance initialized: {_currentBalance}");
            }
            else
            {
                throw new ArgumentException($"Failed to parse balance value: {balance}");
            }
        }
        else
        {
            throw new ArgumentException("Failed to retrieve current balance.");
        }
    }



    private bool CheckIfWonAndUpdateBalance()
    {
        string? balance = GetCurrentBalance();

        if (balance is not null)
        {
            if (double.TryParse(balance, out double balanceValue))
            {
                bool won = balanceValue > _currentBalance;
                _currentBalance = balanceValue;

                _highestBalance = Math.Max(balanceValue, _highestBalance);

                if (won)
                {
                    LogCustom?.Invoke("Bet won!", "#40D63A");
                    if (!_settingsConfig!.IsBetOnOption_1) LogCustom?.Invoke($"Highest Balance is {_highestBalance}", "#40D63A");
                }
                else
                {
                    LogCustom?.Invoke("Bet lost", "#D6C43A");
                }                

                return won;
            }
            else
            {
                throw new ArgumentException($"Failed to parse balance value: {balance}");
            }
        }
        else
        {
            throw new ArgumentException("Failed to retrieve current balance.");
        }        
    }

    private double AccountBalance()
    {
        string? balance = GetCurrentBalance();

        if (balance is not null)
        {
            if (double.TryParse(balance, out double balanceValue))
            {
                return balanceValue;
            }
            else
            {
                throw new ArgumentException($"Failed to parse balance value: {balance}");
            }
        }
        else
        {
            throw new ArgumentException("Failed to retrieve current balance.");
        }
    }



    private string? GetCurrentBalance()
    {
        ArgumentNullException.ThrowIfNull(_driver);
        try
        {            
            // Wrap your async JS with a callback (Selenium requirement)
            var asyncJs = @"
                            var done = arguments[arguments.length-1];
                            (async () => {
                                if (!window.requestBalance) { done(null); return; }
                                await window.requestBalance();
                                const P = window.Pandora || window.o || window.gameProxy || window.game;
                                const balance = P?.store?.game?.balance;
                                const value = balance && typeof balance.get === 'function' ? balance.get() : balance;
                                done(value);
                            })();
                        ";

            var balanceOBJ = ((IJavaScriptExecutor)_driver).ExecuteAsyncScript(asyncJs);

            string balance = balanceOBJ?.ToString() ?? "not_found";            

            if (balance.StartsWith("error:") || balance == "not_found")
            {                
                return null;
            }     

            return balance;
        }
        catch
        {                        
            return null;
        }
    }
}
