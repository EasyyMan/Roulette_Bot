using BackEndCore.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVVM_Core.Models;

public partial class UISettings : ObservableObject
{
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;
}