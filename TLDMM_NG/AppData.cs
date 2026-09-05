using System;
using System.IO;

public static class AppData
{
    private const string AppName = "TLDMM_NG";
    
    public static string LocalFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    
    public static string CacheFolder => Path.Combine(LocalFolder, "Cache");
    
    public static string SettingsFolder => Path.Combine(LocalFolder, "Settings");
    
    public static string RoamingFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppName);
    
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(CacheFolder);
        Directory.CreateDirectory(SettingsFolder);
    }
}