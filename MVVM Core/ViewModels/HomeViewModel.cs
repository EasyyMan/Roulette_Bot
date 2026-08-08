using BackEndCore;
using BackEndCore.Models;
using BackEndCore.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MVVM_Core.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings;
    private readonly ICasinoSiteService? _casinoSite;

    private readonly IUIService _uiService;
        
    private CancellationTokenSource? _cts;
    private Task? _botTask;

    // This collection is exposed directly.
    // CommunityToolkit.Mvvm + ObservableCollection = perfect for logs!
    // No [ObservableProperty] needed because the collection reference never changes.
    public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

    public HomeViewModel(SettingsViewModel settings, ICasinoSiteService? casinoSite, IUIService uiService)
    {        
        ArgumentNullException.ThrowIfNull(uiService);    
        _settings = settings;
        _casinoSite = casinoSite;
        _uiService = uiService;

        // If a casino site service is provided, assign this ViewModel's logging methods
        // so the service can invoke logs back into the UI.
        if (_casinoSite is not null)
        {
            _casinoSite.LogInfo = AddNormalLog;
            _casinoSite.LogError = AddErrorLog;
            _casinoSite.LogGoodNews = AddGoodNewsLog;
            _casinoSite.LogCustom = AddCustomLog;
        }
    }    

    [ObservableProperty]
    private bool _isBotRunning;

     
    // Example methods you can call to toggle it
    public void DisableSettingsUi() => _settings.IsUiEnabled = false;
    public void EnableSettingsUi() => _settings.IsUiEnabled = true;


    private SettingsConfigured GetConfiguredSettings()
    {       
        bool isRealPlay = _settings.PlayModeSelected == PlayMode.RealPlay;
        string email = _settings.CurrentSettings.Email;
        string password = _settings.CurrentSettings.Password;
        bool betRandomBetsEveryTime = _settings.RandomBetMode == RandomBetType.EveryTime;
        bool isBetOnOption_1 = _settings.BetOnMode == BetOn.Option_1;
        bool isChipAmountCalc_Option_1 = _settings.ChipAmountCalcMode == ChipAmountCalc.Option_1;

        //Stop Operating After
        bool isDurationOfTime = _settings.StopOperatingAfterSelectedMode == StopOperatingAfterMode.DurationOfTime;
        int duration = _settings.Duration;
        int dollar = _settings.Dollar;
        bool isInMinutes = _settings.SelectedDurationMode == TimeUnit.Minutes;

        var stopOperatingAfter = new StopOperatingAfterSettings( isDurationOfTime, duration, dollar, isInMinutes );

        //Then Start Operating Again After
        bool isRandomInterval = _settings.ThenStartOperatingAgainAfterSelectedMode == ThenStartOperatingAgainAfterMode.RandomInterval;
        int btwn1 = _settings.Btwn1;
        int btwn2 = _settings.Btwn2;
        int fixedEvery = _settings.FixedEvery;
        bool isInMinutes2 = _settings.SelectedTimeSet2Mode == TimeUnit.Minutes;

        var thenStartOperatingAgainAfter = new ThenStartOperatingAgainAfterSettings(isRandomInterval, btwn1, btwn2, fixedEvery, isInMinutes2);

        BackEndCore.BetOn12section betOn12sectionMode = (BackEndCore.BetOn12section)Enum.Parse(typeof(BackEndCore.BetOn12section), _settings.BetOn12sectionMode.ToString());
        BackEndCore.RandomBet_12section randomBet12sectionMode = (BackEndCore.RandomBet_12section)Enum.Parse(typeof(BackEndCore.RandomBet_12section), _settings.RandomBet_12SectionMode.ToString());
        BackEndCore.ChipAmountCalc_12section chipAmountCalc12sectionMode = (BackEndCore.ChipAmountCalc_12section)Enum.Parse(typeof(BackEndCore.ChipAmountCalc_12section), _settings.ChipAmountCalc_12sectionMode.ToString());

        int resetMarkAmount = _settings.ResetMarkAmount;

        var config = new SettingsConfigured(isRealPlay, 
                                            email, 
                                            password,
                                            betRandomBetsEveryTime, 
                                            isBetOnOption_1, 
                                            isChipAmountCalc_Option_1,
                                            resetMarkAmount,
                                            betOn12sectionMode,
                                            randomBet12sectionMode,
                                            chipAmountCalc12sectionMode,
                                            stopOperatingAfter,
                                            thenStartOperatingAgainAfter);

        return config;       
    }

    [RelayCommand]
    public async Task StartBot()
    {
        if (_botTask?.IsCompleted == false) return;

        AddNormalLog("Starting Bot...");
        IsBotRunning = true;
        DisableSettingsUi();

        var config = GetConfiguredSettings();
        _cts = new CancellationTokenSource();

        try
        {                        
            // We AWAIT here so that this method 'stays alive' to catch errors
            _botTask = Task.Run(() => _casinoSite!.StartAsync(_uiService.GetNewOverlay(),
                                                              config, 
                                                              _settings.SkipEditingYellowBoxes(),
                                                              _cts.Token),
                                                              _cts.Token);
            await _botTask;
        }
        catch (OperationCanceledException)
        {
            AddNormalLog("Bot stopped by user.");
        }
        catch (Exception ex)
        {
            // THIS will now correctly catch crashes that happen during the loop!
            AddErrorLog($"Bot crashed: {ex.Message}");  //AddErrorLog($"An error occurred: {ex.Message}\n\nStackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                AddErrorLog($"InnerException: {ex.InnerException.Message}");
            }
            
            //AddErrorLog($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            // Centralized cleanup: This runs if the bot finishes, crashes, or is cancelled.
            await StopBot();
        }
    } 

    [RelayCommand]
    public async Task StopBot()
    {
        AddNormalLog("Stopping Bot...");
        // If it's already stopping or stopped, just ensure UI is reset
        if (_cts == null)
        {
            IsBotRunning = false;
            EnableSettingsUi();
            return;
        }

        try
        {
            _cts.Cancel();
            // We check if _botTask exists and isn't finished before awaiting
            if (_botTask != null && !_botTask.IsCompleted)
            {
                await _botTask;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AddErrorLog($"Error during Stopping Bot: {ex.Message}");

            if (ex.InnerException != null)
            {
                AddErrorLog($"InnerException: {ex.InnerException.Message}");
            }            
        }
        finally
        {
            _casinoSite!.Stop();
            _cts?.Dispose();
            _cts = null;
            _botTask = null;            
            _settings.CheckYellowBoxesConfigFile();
            EnableSettingsUi();
            IsBotRunning = false;
            AddNormalLog("Bot Stopped.");
        }
    }

    public void AddNormalLog(string message)
    {
        var timestamp = $"({DateTime.Now})";

        LogEntries.Add(new LogEntry
        {
            Text = "• " + message,
            Foreground = "#F2F3F5",
            FontSize = 14,
            FontWeight = "Normal",
            Timestamp = timestamp            
        });
    }

    public void AddErrorLog(string message)
    {
        var timestamp = $"({DateTime.Now})";
        LogEntries.Add(new LogEntry
        {
            Text = "• " + message,
            Foreground = "#FF0000",// Red
            FontSize = 14,
            FontWeight = "SemiBold",
            Timestamp = timestamp
        });        
    }

    public void AddGoodNewsLog(string message)
    {
        var timestamp = $"({DateTime.Now})";
        LogEntries.Add(new LogEntry
        {
            Text = "• " + message,
            Foreground = "#23A559",// green
            FontSize = 14,
            FontWeight = "Normal",
            Timestamp = timestamp
        });        
    }

    public void AddCustomLog(string message, string color)
    {
        var timestamp = $"({DateTime.Now})";
        LogEntries.Add(new LogEntry
        {
            Text = "• " + message,
            Foreground = color,
            FontSize = 14,
            FontWeight = "Normal",
            Timestamp = timestamp
        });
    }

    public void ClearLog() => LogEntries.Clear();    
}


public class LogEntry
{
    public string Text { get; set; } = string.Empty;           // main message
    public string Timestamp { get; set; } = string.Empty;      // ← NEW

    public string Foreground { get; set; } = "#FFFFFF";
    public double FontSize { get; set; } = 14;
    public string FontWeight { get; set; } = "Normal";

    // Timestamp styling (you can change defaults if you want)
    public string TimestampForeground { get; set; } = "#B5BAC1";
    public double TimestampFontSize { get; set; } = 12;
}