using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace TLD_Mod_Manager;

public class InstalledModInfo
{
    public string Version { get; set; } = "";
    public List<string> Files { get; set; } = new();
}

public class Settings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TLDModManager",
        "settings.json");

    public string GamePath { get; set; } = "";
    public Dictionary<string, InstalledModInfo> InstalledMods { get; set; } = new();

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
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
    
    public static string? DetectGamePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\TheLongDark",
                @"C:\Program Files\Steam\steamapps\common\TheLongDark",
            };
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "tld.exe")))
                    return path;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                var possiblePaths = new[]
                {
                    Path.Combine(home, ".steam", "steam", "steamapps", "common", "TheLongDark"),
                    Path.Combine(home, ".local", "share", "Steam", "steamapps", "common", "TheLongDark"),
                };
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "tld")))
                        return path;
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                var path = Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "common", "TheLongDark", "TheLongDark.app", "Contents", "MacOS");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "TheLongDark")))
                    return path;
            }
        }
        return null;
    }
}