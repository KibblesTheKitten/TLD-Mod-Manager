using System.Collections.Generic;

namespace TLD_Mod_Manager;

public class Mod
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string TestedOn { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
    public bool TftftDlcRequired { get; set; }
    public bool IsLibrary { get; set; }
    public bool IsPlugin { get; set; }
    
    public bool IsInstalled { get; set; } = false;
    
    public string? SourceUrl { get; set; }
}