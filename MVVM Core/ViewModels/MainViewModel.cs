using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MVVM_Core.ViewModels;

public enum MenuBtnMode { Home, Settings }

public partial class MainViewModel : ObservableObject
{
   
    [ObservableProperty]
    private MenuBtnMode _menuSelectedMode;

    [ObservableProperty]
    public partial object CurrentView { get; set; }

    public HomeViewModel HomeVM { get; set; }

    public SettingsViewModel SettingsVM { get; set; }

    public MainViewModel(SettingsViewModel settingsVM, HomeViewModel homeVM)
    {
        ArgumentNullException.ThrowIfNull(settingsVM);
        ArgumentNullException.ThrowIfNull(homeVM);        

        SettingsVM = settingsVM;
        HomeVM = homeVM;        

        CurrentView = HomeVM;
        MenuSelectedMode = MenuBtnMode.Home;
        HomeVM.AddGoodNewsLog("Bot resources initialized Successfully.");              
    }

    [RelayCommand]
    public void ShowHomeView()
    {
        CurrentView = HomeVM;
        MenuSelectedMode = MenuBtnMode.Home;
    }

    [RelayCommand]
    public void ShowSettingsView()
    {
        CurrentView = SettingsVM;
        MenuSelectedMode = MenuBtnMode.Settings;
    }
}

