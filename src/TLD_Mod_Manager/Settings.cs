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
}

public class InstallLocationDetection

{
    public static string? DetectGamePath()
    {
        var possiblePaths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            possiblePaths.Add(@"C:\Program Files (x86)\Steam\steamapps\common\TheLongDark");
            possiblePaths.Add(@"C:\Program Files\Steam\steamapps\common\TheLongDark");
            possiblePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "TheLongDark"));
            var steamLibraries = GetSteamLibraries();
            possiblePaths.AddRange(steamLibraries);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                possiblePaths.Add(Path.Combine(home, ".steam", "steam", "steamapps", "common", "TheLongDark"));
                possiblePaths.Add(Path.Combine(home, ".steam", "root", "steamapps", "common", "TheLongDark"));
                possiblePaths.Add(Path.Combine(home, ".local", "share", "Steam", "steamapps", "common", "TheLongDark"));
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                possiblePaths.Add(Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "common", "TheLongDark"));
            }
        }

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path) && (File.Exists(Path.Combine(path, "tld.exe")) || File.Exists(Path.Combine(path, "tld"))))
            {
                return path;
            }
        }
        return null;
    }
    
    private static List<string> GetSteamLibraries()
    {
        var libraries = new List<string>();
        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath)) return libraries;

        var libraryFoldersFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersFile)) return libraries;

        var lines = File.ReadAllLines(libraryFoldersFile);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("\"path\""))
            {
                var parts = trimmed.Split('"');
                if (parts.Length >= 4)
                {
                    var path = parts[3];
                    path = path.Replace("\\\\", "\\");
                    if (Directory.Exists(path))
                    {
                        libraries.Add(Path.Combine(path, "steamapps", "common"));
                    }
                }
            }
        }
        var defaultCommon = Path.Combine(steamPath, "steamapps", "common");
        if (Directory.Exists(defaultCommon))
            libraries.Add(defaultCommon);
        return libraries;
    }
    
    private static string? GetSteamInstallPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                if (key?.GetValue("InstallPath") is string path)
                    return path;
            }
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key?.GetValue("InstallPath") is string path)
                    return path;
            }
            var common = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam"
            };
            foreach (var dir in common)
            {
                if (Directory.Exists(dir))
                    return dir;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                var candidates = new[]
                {
                    Path.Combine(home, ".steam", "steam"),
                    Path.Combine(home, ".steam", "root"),
                    Path.Combine(home, ".local", "share", "Steam")
                };
                foreach (var dir in candidates)
                {
                    if (Directory.Exists(dir))
                        return dir;
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                var path = Path.Combine(home, "Library", "Application Support", "Steam");
                if (Directory.Exists(path))
                    return path;
            }
        }
        return null;
    }
}