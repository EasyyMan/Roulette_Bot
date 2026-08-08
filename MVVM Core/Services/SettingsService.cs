using MVVM_Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MVVM_Core.Services;

public class SettingsService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public UISettings Load()
    {
        if (!File.Exists(_filePath))
            return new UISettings();

        string json = File.ReadAllText(_filePath);
        var root = JsonNode.Parse(json) as JsonObject;

        var section = root?["UISettings"]?.ToJsonString();
        return section is not null
            ? JsonSerializer.Deserialize<UISettings>(section) ?? new UISettings()
            : new UISettings();
    }

    public void Save(UISettings settings)
    {
        string json = File.Exists(_filePath) ? File.ReadAllText(_filePath) : "{}";
        var root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();

        root["UISettings"] = JsonSerializer.SerializeToNode(settings);

        File.WriteAllText(_filePath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
