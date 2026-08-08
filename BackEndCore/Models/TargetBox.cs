using CommunityToolkit.Mvvm.ComponentModel;

namespace BackEndCore.Models;


public partial class TargetBox : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    public string HexColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double X { get; set; }

    [ObservableProperty]
    public partial double Y { get; set; }

    [ObservableProperty]
    public partial double Width { get; set; }

    [ObservableProperty]
    public partial double Height { get; set; }

    public TargetBox(string name, string color, double x, double y, double w, double h)
    {
        Name = name;
        HexColor = color;
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    // Parameterless constructor required for JSON deserialization
    public TargetBox() { }
}