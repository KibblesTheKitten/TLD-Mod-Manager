using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SharpCompress.Archives;

namespace TLD_Mod_Manager;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private Settings _settings = Settings.Load();

    public string GamePath
    {
        get => _settings.GamePath;
        set
        {
            _settings.GamePath = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    private bool _isGamePathDetected;
    public bool IsGamePathDetected
    {
        get => _isGamePathDetected;
        set { _isGamePathDetected = value; OnPropertyChanged(); }
    }
    
    private HashSet<string> _installedModNames = new();

    public bool IsModInstalled(Mod mod) => _installedModNames.Contains(mod.Name);

    public void MarkModInstalled(Mod mod)
    {
        _installedModNames.Add(mod.Name);
        mod.IsInstalled = true;
    }
    
    private ObservableCollection<Mod> _mods = new();
    public ObservableCollection<Mod> Mods
    {
        get => _mods;
        set
        {
            _mods = value;
            OnPropertyChanged();
            FilterMods();
        }
    }

    private ObservableCollection<Mod> _filteredMods = new();
    public ObservableCollection<Mod> FilteredMods
    {
        get => _filteredMods;
        set
        {
            _filteredMods = value;
            OnPropertyChanged();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterMods();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    private ModDetails? _selectedModDetails;
    public ModDetails? SelectedModDetails
    {
        get => _selectedModDetails;
        set
        {
            _selectedModDetails = value;
            OnPropertyChanged();
        }
    }
    
    public AsyncCommand<Mod> ShowDetailsCommand { get; }
    public AsyncCommand<Mod> InstallCommand { get; }
    public AsyncCommand SelectGamePathCommand { get; }
    public AsyncCommand AcceptGamePathCommand { get; }
    public AsyncCommand ChangeGamePathCommand { get; }
    public AsyncCommand InstallMelonLoaderCommand { get; }
    
    public MainWindowViewModel()
    {
        ShowDetailsCommand = new AsyncCommand<Mod>(ShowDetails);
        InstallCommand = new AsyncCommand<Mod>(InstallMod);
        SelectGamePathCommand = new AsyncCommand(SelectGamePath);
        AcceptGamePathCommand = new AsyncCommand(AcceptGamePath);
        ChangeGamePathCommand = SelectGamePathCommand;
        InstallMelonLoaderCommand = new AsyncCommand(InstallMelonLoader);

        _ = LoadModsAsync();
    }
    
    private void FilterMods()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredMods = new ObservableCollection<Mod>(Mods);
        }
        else
        {
            var filtered = Mods.Where(m =>
                (m.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (m.Author?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (m.Categories?.Any(c => c.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ?? false)
            ).ToList();
            FilteredMods = new ObservableCollection<Mod>(filtered);
        }
    }
    
    private async Task LoadModsAsync()
    {
        IsLoading = true;
        try
        {
            var service = new ModService();
            var loadedModsList = await service.GetModsAsync();
            Mods = new ObservableCollection<Mod>(loadedModsList);
            
            _ = FetchAllDetailsAsync(loadedModsList);
            
            if (string.IsNullOrEmpty(GamePath))
            {
                var detectedPath = Settings.DetectGamePath();
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    GamePath = detectedPath;
                    IsGamePathDetected = true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading mods: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task FetchAllDetailsAsync(List<Mod> mods)
    {
        var semaphore = new SemaphoreSlim(5);
        var tasks = mods.Select(async mod =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(mod.SourceUrl)) return;
                var service = new ModService();
                var details = await service.GetModDetailsAsync(mod.SourceUrl);
                if (details != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        mod.Status = details.Status;
                        mod.TestedOn = details.TestedOn;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching details for {mod.Name}: {ex}");
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
    
    private async Task ShowDetails(Mod? mod)
    {
        if (mod == null) return;

        var detailsWindow = new DetailsWindow
        {
            DataContext = mod
        };

        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await detailsWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            detailsWindow.Show();
        }
    }
    
    private async Task InstallMod(Mod? mod)
    {
        if (mod == null) return;
        if (string.IsNullOrEmpty(mod.DownloadUrl))
        {
            await ShowMessage("Error", "No download URL for this mod.");
            return;
        }
        
        if (string.IsNullOrEmpty(GamePath))
        {
            await ShowMessage("Game Path Required", "Please select your The Long Dark installation folder first.");
            return;
        }

        await InstallModWithDependencies(mod, new HashSet<string>());
    }
    
    private async Task InstallModWithDependencies(Mod mod, HashSet<string> processing)
    {
        if (processing.Contains(mod.Name))
        {
            await ShowMessage("Dependency Loop", $"Circular dependency detected: {mod.Name}");
            return;
        }
        processing.Add(mod.Name);
        
        foreach (var depName in mod.Dependencies)
        {
            var depMod = Mods.FirstOrDefault(m => m.Name == depName);
            if (depMod == null)
            {
                await ShowMessage("Missing Dependency", $"Dependency '{depName}' not found in mod list.");
                continue;
            }

            if (!IsModInstalled(depMod))
            {
                await InstallModWithDependencies(depMod, processing);
            }
        }
        
        await DownloadAndInstallMod(mod);
        MarkModInstalled(mod);
    }
    
    private async Task DownloadAndInstallMod(Mod mod)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(mod.DownloadUrl);
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(new Uri(mod.DownloadUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{mod.Name}.dll";

            var tempFile = Path.Combine(Path.GetTempPath(), fileName);
            using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            var modsFolder = Path.Combine(GamePath, "Mods");
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension == ".dll")
            {
                var destFile = Path.Combine(modsFolder, fileName);
                File.Copy(tempFile, destFile, true);
            }
            else if (extension == ".modcomponent")
            {
                var destFile = Path.Combine(modsFolder, fileName);
                File.Copy(tempFile, destFile, true);
            }
            else if (extension == ".zip")
            {
                using var archive = ZipFile.OpenRead(tempFile);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destPath = Path.Combine(modsFolder, entry.Name);
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                    
                    entry.ExtractToFile(destPath, true);
                }
            }
            else if (extension == ".7z" || extension == ".7zip")
            {
                using var archive = ArchiveFactory.OpenArchive(tempFile);
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory) continue;
                    
                    var destPath = Path.Combine(modsFolder, Path.GetFileName(entry.Key));
                    using var entryStream = entry.OpenEntryStream();
                    using var fileStream = File.Create(destPath);
                    entryStream.CopyTo(fileStream);
                }
            }
            else
            {
                // For other files. just plop them in mods and pray xd
                var destFile = Path.Combine(modsFolder, fileName);
                File.Copy(tempFile, destFile, true);
                await ShowMessage("Warning", $"Unknown file type {extension} for {mod.Name}. Copied as-is to Mods folder.");
            }

            File.Delete(tempFile);
            await ShowMessage("Success", $"Installed {mod.Name}");
        }
        catch (Exception ex)
        {
            await ShowMessage("Install Failed", $"Error installing {mod.Name}: {ex.Message}");
        }
    }
    
    private async Task SelectGamePath()
    {
        if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "Select The Long Dark installation folder"
        };
        var folder = await dialog.ShowAsync(desktop.MainWindow);
        if (!string.IsNullOrEmpty(folder))
        {
            GamePath = folder;
            IsGamePathDetected = true;
            await ShowMessage("Game Path Set", $"Game path set to: {folder}");
        }
    }
    private async Task AcceptGamePath()
    {
        IsGamePathDetected = true;
        await ShowMessage("Game Path Accepted", $"Using game path: {GamePath}");
    }
    
    private async Task ShowMessage(string title, string message)
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard(title, message);
        await msgBox.ShowAsync();
    }
    
    // ==================== MelonLoader Installation ====================
private bool IsMelonLoaderInstalled()
{
    if (string.IsNullOrEmpty(GamePath)) return false;
    // Common MelonLoader artifacts: version.dll (Windows) or version.dylib (macOS), and MelonLoader folder
    return File.Exists(Path.Combine(GamePath, "version.dll")) ||
           File.Exists(Path.Combine(GamePath, "version.dylib")) ||
           Directory.Exists(Path.Combine(GamePath, "MelonLoader"));
}

private async Task InstallMelonLoader()
{
    if (string.IsNullOrEmpty(GamePath))
    {
        await ShowMessage("Game Path Required", "Please select or confirm your The Long Dark installation folder first.");
        return;
    }

    if (IsMelonLoaderInstalled())
    {
        var confirm = await ShowConfirmDialog("MelonLoader Installed", 
            "MelonLoader appears to be already installed. Do you want to reinstall?");
        if (!confirm) return;
    }

    await ShowMessage("Downloading MelonLoader", "This may take a moment...");

    try
    {
        // Use a specific MelonLoader version (adjust as needed)
        // You can fetch the latest dynamically, but for reliability, we'll use a known version.
        string melonLoaderZipUrl = "https://github.com/LavaGang/MelonLoader/releases/download/v0.7.2/MelonLoader.zip";

        using var client = new HttpClient();
        var response = await client.GetAsync(melonLoaderZipUrl);
        response.EnsureSuccessStatusCode();

        var tempZip = Path.Combine(Path.GetTempPath(), "MelonLoader.zip");
        using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await response.Content.CopyToAsync(fs);
        }

        // Extract to game root
        using var archive = ZipFile.OpenRead(tempZip);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // skip directories

            var destPath = Path.Combine(GamePath, entry.FullName);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            entry.ExtractToFile(destPath, true);
        }

        File.Delete(tempZip);

        await ShowMessage("Success", "MelonLoader has been installed.");
    }
    catch (Exception ex)
    {
        await ShowMessage("Install Failed", $"Error installing MelonLoader: {ex.Message}");
    }
}

private async Task<bool> ShowConfirmDialog(string title, string message)
{
    var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo);
    var result = await box.ShowAsync();
    return result == ButtonResult.Yes;
}
}