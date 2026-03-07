using System;
using System.IO;
using System.Text.Json;

namespace TLD_Mod_Manager;

public class Settings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TLDModManager",
        "settings.json");

    public string GamePath { get; set; } = "";

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this);
        File.WriteAllText(SettingsPath, json);
    }

    public static Settings Load()
    {
        if (File.Exists(SettingsPath))
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
        return new Settings();
    }
}