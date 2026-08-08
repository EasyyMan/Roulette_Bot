using MVVM_Core.ViewModels;
using System.Windows;

namespace Roulette_Bot;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}