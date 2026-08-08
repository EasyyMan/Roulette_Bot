using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MVVM_Core.ViewModels;

public enum MenuBtnMode { Home, Settings }

public partial class MainViewModel : ObservableObject
{
   
    [ObservableProperty]
    private MenuBtnMode _menuSelectedMode;
   
    public HomeViewModel HomeVM { get; set; }

    public SettingsViewModel SettingsVM { get; set; }

    public MainViewModel(SettingsViewModel settingsVM, HomeViewModel homeVM)
    {
        ArgumentNullException.ThrowIfNull(settingsVM);
        ArgumentNullException.ThrowIfNull(homeVM);        

        SettingsVM = settingsVM;
        HomeVM = homeVM;        
        
        MenuSelectedMode = MenuBtnMode.Home;
        HomeVM.AddGoodNewsLog("Bot resources initialized Successfully.");              
    }

    [RelayCommand]
    public void ShowHomeView()
    {        
        MenuSelectedMode = MenuBtnMode.Home;
    }

    [RelayCommand]
    public void ShowSettingsView()
    {        
        MenuSelectedMode = MenuBtnMode.Settings;
    }
}

