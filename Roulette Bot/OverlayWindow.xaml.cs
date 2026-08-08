using BackEndCore.Models;
using BackEndCore.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Roulette_Bot;


public partial class OverlayWindow : Window, IUIOverlayService
{   
    private const string ConfigFile = "Yellow_Boxes_Locations.json";
    
    private bool _isLocked = false;

    public ObservableCollection<TargetBox> TargetBoxes { get; set; } = new();

    // Win32 API to make the window click-through
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public OverlayWindow()
    {
        InitializeComponent();
        
        DataContext = this;               
    }

    // Handles moving the entire box
    private void Body_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TargetBox box)
        {
            box.X += e.HorizontalChange;
            box.Y += e.VerticalChange;
        }
    }

    // Handles resizing the box from the bottom right corner
    private void Resize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TargetBox box)
        {
            double newWidth = box.Width + e.HorizontalChange;
            double newHeight = box.Height + e.VerticalChange;

            if (newWidth > 15) box.Width = newWidth;
            if (newHeight > 15) box.Height = newHeight;
        }
    }

    // Locks the UI and triggers the bot logic
    private void LockAndStart_Click(object sender, RoutedEventArgs e)
    {
        // 1. Make the window invisible to mouse clicks
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);

        // 2. Hide the dark background and the control panel
        this.Background = System.Windows.Media.Brushes.Transparent;
        //((FrameworkElement)sender).Parent.Visibility = Visibility.Collapsed;

        // 3. Save coordinates and start the bot loop in the ViewModel
        BeginAutomationPhase();

        Close();        
    }


    public void LoadBoxesConfiguration()
    {
        if (File.Exists(ConfigFile))
        {
            string json = File.ReadAllText(ConfigFile);
            var savedBoxes = JsonSerializer.Deserialize<ObservableCollection<TargetBox>>(json);
            if (savedBoxes != null)
            {
                Dispatcher.Invoke(() =>
                {
                    TargetBoxes.Clear();
                    foreach (var item in savedBoxes)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                        TargetBox box = new TargetBox(item.Name, item.HexColor, item.X, item.Y, item.Width, item.Height);
                        TargetBoxes.Add(box);
                    }
                });
                               
                return;
            }
        }

        Dispatcher.Invoke(() =>
        {
            // If no save file exists, generate the default 11 boxes based on your image
            TargetBoxes.Clear();
            TargetBoxes.Add(new TargetBox("Quick Spin", "#FFFF00", 20, 260, 50, 50));

            TargetBoxes.Add(new TargetBox("1-18", "#FFFF00", 800, 480, 50, 30));
            TargetBoxes.Add(new TargetBox("Even", "#FFFF00", 880, 480, 50, 30));
            TargetBoxes.Add(new TargetBox("Red", "#FFFF00", 960, 480, 50, 30));
            TargetBoxes.Add(new TargetBox("Black", "#FFFF00", 1040, 480, 50, 30));
            TargetBoxes.Add(new TargetBox("Odd", "#FFFF00", 1120, 480, 50, 30));
            TargetBoxes.Add(new TargetBox("19-36", "#FFFF00", 1200, 480, 50, 30));

            TargetBoxes.Add(new TargetBox("1st 12", "#FFFF00", 800, 380, 50, 30));
            TargetBoxes.Add(new TargetBox("2nd 12", "#FFFF00", 880, 380, 50, 30));
            TargetBoxes.Add(new TargetBox("3rd 12", "#FFFF00", 960, 380, 50, 30));

            TargetBoxes.Add(new TargetBox("top 2:1", "#FFFF00", 1200, 328, 50, 30));
            TargetBoxes.Add(new TargetBox("mid 2:1", "#FFFF00", 1200, 354, 50, 30));            
            TargetBoxes.Add(new TargetBox("bottom 2:1", "#FFFF00", 1200, 374, 50, 30));

            //TargetBoxes.Add(new TargetBox("Clear Bets", "#FFFF00", 350, 780, 60, 60));

            TargetBoxes.Add(new TargetBox("Chip 1", "#FFFF00", 550, 580, 60, 60));
            TargetBoxes.Add(new TargetBox("Chip 5", "#FFFF00", 650, 580, 60, 60));
            TargetBoxes.Add(new TargetBox("Chip 25", "#FFFF00", 750, 580, 60, 60));
            TargetBoxes.Add(new TargetBox("Chip 100", "#FFFF00", 850, 580, 60, 60));
            TargetBoxes.Add(new TargetBox("Chip 500", "#FFFF00", 950, 580, 60, 60));

            TargetBoxes.Add(new TargetBox("Spin", "#FFFF00", 1150, 580, 80, 80));
            TargetBoxes.Add(new TargetBox("Rebet/x2", "#FFFF00", 1050, 580, 60, 60));

        });        
    }

    public void SaveConfiguration()
    {
        string json = JsonSerializer.Serialize(TargetBoxes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
        _isLocked = true;
    }

    // This is called when the user hits "Lock & Play" on the overlay
    public void BeginAutomationPhase()
    {
        SaveConfiguration();
        

        // Now that positions are locked and saved, you can iterate over TargetBoxes 
        // and use TargetBoxes[i].X and TargetBoxes[i].Y to drive your Mouse clicker bot

        // ExecuteBotLogic(TargetBoxes);
    }



    public void ShowOverlayWindow()
    {
        LoadBoxesConfiguration();
        
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => Show());
            return;
        }        
    }

    public void CloseOverlayWindow()
    {        
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => Close());
            return;
        }                
    }

    public bool IsLocked()
    {
        return _isLocked;
    }

    public ObservableCollection<TargetBox> GetTargetBoxes()
    {
        return TargetBoxes;
    }
}
