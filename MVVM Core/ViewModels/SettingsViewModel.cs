using BackEndCore.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Core.Models;
using MVVM_Core.Services;
using System.ComponentModel;

namespace MVVM_Core.ViewModels;

public enum StopOperatingAfterMode { DurationOfTime, DollarAmountWon }

public enum ThenStartOperatingAgainAfterMode { FixedInterval, RandomInterval }

public enum PlayMode { RealPlay, PracticePlay }


public enum TimeUnit
{
    Minutes,
    Hours
}

public enum ConfigYellowBoxes
{
    Enabled,
    Disabled
}

public enum RandomBetType
{
    EveryTime,
    During_Wins
}


public enum BetOn
{
    Option_1,
    Option_2
}

public enum ChipAmountCalc
{
    Option_1,
    Option_2
}

public enum BetOn12section
{
    BetOn_Both_Sets,
    BetOn_1_Set_At_A_Time,
    BetOn_Both_Sets_1_Set_With_1_Bet
}


public enum RandomBet_12section
{
    BetOn_SameBets_EveryTime,
    BetOn_RandomBets_EveryTime,
    BetOn_RandomBets_DuringWins
}

public enum ChipAmountCalc_12section
{
    SimpleDouble,
    DoublePlusOne,
    DoublePlusIncrementingDollar,
    DoubleThenTriple
}


public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private UISettings _backup = new();



    //============================================================================
    //Stop Operating After

    [ObservableProperty]
    private StopOperatingAfterMode _stopOperatingAfterSelectedMode;

    [ObservableProperty]
    private int _duration = 8;

    [ObservableProperty]
    private TimeUnit _selectedDurationMode = TimeUnit.Minutes;

    public List<TimeUnit> TimeUnits { get; } = new() { TimeUnit.Minutes, TimeUnit.Hours };



    [ObservableProperty]
    private int _dollar = 6;   // default value shown in the TextBox

    //============================================================================
    //Then Start Operating Again After

    [ObservableProperty]
    private ThenStartOperatingAgainAfterMode _thenStartOperatingAgainAfterSelectedMode;

    [ObservableProperty]
    private int _btwn1 = 4;   // default matches your Text="4"


    [ObservableProperty]
    private int _btwn2 = 8;   // default matches your Text="8"


    [ObservableProperty]
    private int _fixedEvery = 8;   // default matches your Text="8"

    
    [ObservableProperty]
    private TimeUnit _selectedTimeSet2Mode = TimeUnit.Minutes;
     

    //============================================================================

    // This property controls the entire grid
    [ObservableProperty]
    private bool _isUiEnabled = true;     // default = enabled

    [ObservableProperty]
    private UISettings _currentSettings = new();

    [ObservableProperty]
    private bool _isDirty;


    

    [ObservableProperty]
    private bool _isDurationOfTime;
    
     
    [ObservableProperty]
    private PlayMode _playModeSelected;

   
    [ObservableProperty]
    private bool _isRandomInterval;


    [ObservableProperty]
    public partial int ResetMarkAmount { get; set; } = 486;


    #region BetOn

    [ObservableProperty]
    public partial BetOn BetOnMode { get; set; } = BetOn.Option_1;


    [RelayCommand]
    public void Set_BetOn_Option_1()
    {
        BetOnMode = BetOn.Option_1;
    }

    [RelayCommand]
    public void Set_BetOn_Option_2()
    {
        BetOnMode = BetOn.Option_2;
    }

    #endregion

    #region ChipAmountCalc

    [ObservableProperty]
    public partial ChipAmountCalc ChipAmountCalcMode { get; set; } = ChipAmountCalc.Option_1;


    [RelayCommand]
    public void Set_ChipAmountCalc_Option_1()
    {
        ChipAmountCalcMode = ChipAmountCalc.Option_1;
    }

    [RelayCommand]
    public void Set_ChipAmountCalc_Option_2()
    {
        ChipAmountCalcMode = ChipAmountCalc.Option_2;
    }

    #endregion

    #region BetOn12section

    [ObservableProperty]
    public partial BetOn12section BetOn12sectionMode { get; set; } = BetOn12section.BetOn_Both_Sets;

    [RelayCommand]
    public void Set_BetOn12section_Both_Sets()
    {
        BetOn12sectionMode = BetOn12section.BetOn_Both_Sets;
    }
    
    [RelayCommand]
    public void Set_BetOn12section_1_Set_At_A_Time()
    {
        BetOn12sectionMode = BetOn12section.BetOn_1_Set_At_A_Time;
    }

    [RelayCommand]
    public void Set_BetOn12section_Both_Sets_1_Set_With_1_Bet()
    {
        BetOn12sectionMode = BetOn12section.BetOn_Both_Sets_1_Set_With_1_Bet;
    }
    //BetOn_Both_Sets_1_Set_With_1_Bet

    #endregion


    #region RandomBet_12section

    [ObservableProperty]
    public partial RandomBet_12section RandomBet_12SectionMode { get; set; } = RandomBet_12section.BetOn_SameBets_EveryTime;

    [RelayCommand]
    public void Set_RandomBet_12section_SameBets_EveryTime()
    {
        RandomBet_12SectionMode = RandomBet_12section.BetOn_SameBets_EveryTime;
    }

    [RelayCommand]
    public void Set_RandomBet_12section_RandomBets_EveryTime()
    {
        RandomBet_12SectionMode = RandomBet_12section.BetOn_RandomBets_EveryTime;
    }

    [RelayCommand]
    public void Set_RandomBet_12section_RandomBets_DuringWins()
    {
        RandomBet_12SectionMode = RandomBet_12section.BetOn_RandomBets_DuringWins;
    }

    #endregion

    #region ChipAmountCalc_12section

    [ObservableProperty]
    public partial ChipAmountCalc_12section ChipAmountCalc_12sectionMode { get; set; } = ChipAmountCalc_12section.SimpleDouble;

    [RelayCommand]
    public void Set_ChipAmountCalc_12section_SimpleDouble()
    {
        ChipAmountCalc_12sectionMode = ChipAmountCalc_12section.SimpleDouble;
    }

    [RelayCommand]
    public void Set_ChipAmountCalc_12section_DoublePlusOne()
    {
        ChipAmountCalc_12sectionMode = ChipAmountCalc_12section.DoublePlusOne;
    }

    [RelayCommand]
    public void Set_ChipAmountCalc_12section_DoublePlusIncrementingDollar()
    {
        ChipAmountCalc_12sectionMode = ChipAmountCalc_12section.DoublePlusIncrementingDollar;
    }

    [RelayCommand]
    public void Set_ChipAmountCalc_12section_DoubleThenTriple()
    {
        ChipAmountCalc_12sectionMode = ChipAmountCalc_12section.DoubleThenTriple;
    }

    #endregion


    public SettingsViewModel(SettingsService service, IUIService uiService)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(uiService);

        _service = service;
        _uiService = uiService;
        LoadSettings();           // ← automatically loads on startup

        SetDefaultSettings();
        CheckYellowBoxesConfigFile();
    }

    #region Cafe Casino Account Settings
    private void LoadSettings()
    {
        CurrentSettings = _service.Load();
        CreateBackup();

        // Listen for any change in Email or Password
        CurrentSettings.PropertyChanged += CurrentSettings_PropertyChanged;
    }

    private void CreateBackup()
    {
        _backup = new UISettings
        {
            Email = CurrentSettings.Email,
            Password = CurrentSettings.Password,            
        };

        IsDirty = false;
    }

    [RelayCommand]
    private void Save()
    {
        _service.Save(CurrentSettings);
        CreateBackup();             // update backup after save
        // Optional: show "Settings saved" message here
    }

    [RelayCommand]
    private void Cancel()
    {
        // Restore the values the user had before editing
        CurrentSettings.Email = _backup.Email;
        CurrentSettings.Password = _backup.Password;
        IsDirty = false;
    }

    private void CurrentSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UISettings.Email) or nameof(UISettings.Password))
        {
            IsDirty = CurrentSettings.Email != _backup.Email ||
                      CurrentSettings.Password != _backup.Password;
        }      
    }
    #endregion


    private void SetDefaultSettings()
    {
        SelectPracticePlay();
        SelectDollarAmountWon();        
        SelectRandomInterval();        
    }


    #region StopOperatingAfter

    [RelayCommand]
    public void SelectDollarAmountWon()
    {
        StopOperatingAfterSelectedMode = StopOperatingAfterMode.DollarAmountWon;
        IsDurationOfTime = StopOperatingAfterSelectedMode == StopOperatingAfterMode.DurationOfTime;
    }

    [RelayCommand]
    public void SelectDurationOfTime()
    {
        StopOperatingAfterSelectedMode = StopOperatingAfterMode.DurationOfTime;
        IsDurationOfTime = StopOperatingAfterSelectedMode == StopOperatingAfterMode.DurationOfTime;
    }
    #endregion


    #region PlayMode
    [RelayCommand]
    public void SelectRealPlay()
    {
        PlayModeSelected = PlayMode.RealPlay;
    }

    [RelayCommand]
    public void SelectPracticePlay()
    {
        PlayModeSelected = PlayMode.PracticePlay;
    }
    #endregion


    #region Then Start Operating Again After
    [RelayCommand]
    public void SelectFixedInterval()
    {
        ThenStartOperatingAgainAfterSelectedMode = ThenStartOperatingAgainAfterMode.FixedInterval;
        IsRandomInterval = ThenStartOperatingAgainAfterSelectedMode == ThenStartOperatingAgainAfterMode.RandomInterval;
    }

    [RelayCommand]
    public void SelectRandomInterval()
    {
        ThenStartOperatingAgainAfterSelectedMode = ThenStartOperatingAgainAfterMode.RandomInterval;
        IsRandomInterval = ThenStartOperatingAgainAfterSelectedMode == ThenStartOperatingAgainAfterMode.RandomInterval;
    }
    #endregion


    #region Edit Yellow Boxes Positions

    private string _yellowBoxesConfigFile = "Yellow_Boxes_Locations.json";

    private readonly IUIService _uiService;

    [ObservableProperty]
    public partial bool ShowEditYellowBoxesPositionsSection { get; set; }

    [ObservableProperty]
    public partial ConfigYellowBoxes YellowBoxesConfigType { get; set; } = ConfigYellowBoxes.Disabled;

    public void CheckYellowBoxesConfigFile()
    {
        if (File.Exists(_yellowBoxesConfigFile))
        {            
            ShowEditYellowBoxesPositionsSection = true;
            YellowBoxesConfigType = ConfigYellowBoxes.Disabled;
        }
        else
        {            
            ShowEditYellowBoxesPositionsSection = false;
        }
    }

    public bool SkipEditingYellowBoxes() => File.Exists(_yellowBoxesConfigFile);


    [RelayCommand]
    public void EnableEditYellowBoxes()
    {
        try
        {
            if (File.Exists(_yellowBoxesConfigFile))
            {
                File.Delete(_yellowBoxesConfigFile);
            }

            YellowBoxesConfigType = ConfigYellowBoxes.Enabled;
        }
        catch (Exception ex)
        {
            _uiService.ShowErrorMessage("An Error Occurred When You Enabled 'Edit Yellow Boxes Positions'\n\n" + ex.Message);
        }        
    }

    [RelayCommand]
    public void DisableEditYellowBoxes()
    {
        YellowBoxesConfigType = ConfigYellowBoxes.Disabled;        
    }

    #endregion


    #region Implement Random Bet Every Time

    [ObservableProperty]
    private RandomBetType _randomBetMode = RandomBetType.During_Wins;

    [RelayCommand]
    public void SetEveryTimeRandomBet()
    {
        RandomBetMode = RandomBetType.EveryTime;
    }

    [RelayCommand]
    public void SetDuringWinsRandomBet()
    {
        RandomBetMode = RandomBetType.During_Wins;
    }

    #endregion
}