using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TLD_Mod_Manager;

public class Mod : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    private string _version = string.Empty;
    public string Version
    {
        get => _version;
        set { _version = value; OnPropertyChanged(); }
    }

    private string _author = string.Empty;
    public string Author
    {
        get => _author;
        set { _author = value; OnPropertyChanged(); }
    }

    private List<string> _categories = new();
    public List<string> Categories
    {
        get => _categories;
        set { _categories = value; OnPropertyChanged(); }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    private string _imageUrl = string.Empty;
    public string ImageUrl
    {
        get => _imageUrl;
        set { _imageUrl = value; OnPropertyChanged(); }
    }

    private string _downloadUrl = string.Empty;
    public string DownloadUrl
    {
        get => _downloadUrl;
        set { _downloadUrl = value; OnPropertyChanged(); }
    }

    private List<string> _dependencies = new();
    public List<string> Dependencies
    {
        get => _dependencies;
        set { _dependencies = value; OnPropertyChanged(); }
    }

    private string _status = "Unknown";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _testedOn = string.Empty;
    public string TestedOn
    {
        get => _testedOn;
        set { _testedOn = value; OnPropertyChanged(); }
    }

    private string _lastUpdated = string.Empty;
    public string LastUpdated
    {
        get => _lastUpdated;
        set { _lastUpdated = value; OnPropertyChanged(); }
    }

    private bool _tftftDlcRequired;
    public bool TftftDlcRequired
    {
        get => _tftftDlcRequired;
        set { _tftftDlcRequired = value; OnPropertyChanged(); }
    }

    private bool _isLibrary;
    public bool IsLibrary
    {
        get => _isLibrary;
        set { _isLibrary = value; OnPropertyChanged(); }
    }

    private bool _isPlugin;
    public bool IsPlugin
    {
        get => _isPlugin;
        set { _isPlugin = value; OnPropertyChanged(); }
    }

    private string? _sourceUrl;
    public string? SourceUrl
    {
        get => _sourceUrl;
        set { _sourceUrl = value; OnPropertyChanged(); }
    }

    private bool _isInstalled;
    public bool IsInstalled
    {
        get => _isInstalled;
        set { _isInstalled = value; OnPropertyChanged(); }
    }
}