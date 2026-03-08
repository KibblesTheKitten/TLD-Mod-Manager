using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private const string CurrentVersion = "0.0.3";
    private const string GitHubRepo = "KibblesTheKitten/TLD-Mod-Manager";

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
        set
        {
            _isGamePathDetected = value;
            OnPropertyChanged();
        }
    }

    private HashSet<string> _installedModNames = new();

    public bool IsModInstalled(Mod mod) => _installedModNames.Contains(mod.Name);

    public void MarkModInstalled(Mod mod, List<string> installedFiles)
    {
        if (!_installedModNames.Contains(mod.Name))
        {
            _installedModNames.Add(mod.Name);
            _settings.InstalledModFiles[mod.Name] = installedFiles;
            _settings.Save();
            mod.IsInstalled = true;
        }
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
    public AsyncCommand CheckForUpdatesCommand { get; }
    public AsyncCommand<Mod> UninstallCommand { get; }

    public MainWindowViewModel()
    {
        ShowDetailsCommand = new AsyncCommand<Mod>(ShowDetails);
        InstallCommand = new AsyncCommand<Mod>(InstallMod);
        SelectGamePathCommand = new AsyncCommand(SelectGamePath);
        AcceptGamePathCommand = new AsyncCommand(AcceptGamePath);
        ChangeGamePathCommand = SelectGamePathCommand;
        InstallMelonLoaderCommand = new AsyncCommand(InstallMelonLoader);
        CheckForUpdatesCommand = new AsyncCommand(CheckForUpdates);
        UninstallCommand = new AsyncCommand<Mod>(UninstallMod);

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
            LoadInstalledMods();

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
    }

    private void LoadInstalledMods()
    {
        _installedModNames = new HashSet<string>(_settings.InstalledModFiles.Keys);
        foreach (var mod in Mods)
        {
            mod.IsInstalled = _installedModNames.Contains(mod.Name);
        }
    }

    private async Task DownloadAndInstallMod(Mod mod)
    {
        var installedFiles = new List<string>();
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

            // Determine target folder based on mod type
            var targetFolder = mod.IsPlugin ? Path.Combine(GamePath, "Plugins") : Path.Combine(GamePath, "Mods");
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension == ".dll" || extension == ".modcomponent")
            {
                var destFile = Path.Combine(targetFolder, fileName);
                File.Copy(tempFile, destFile, true);
                installedFiles.Add(Path.GetRelativePath(GamePath, destFile));
            }
            else if (extension == ".zip")
            {
                using var archive = ZipFile.OpenRead(tempFile);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    var destPath = Path.Combine(targetFolder, entry.Name);
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                    entry.ExtractToFile(destPath, true);
                    installedFiles.Add(Path.GetRelativePath(GamePath, destPath));
                }
            }
            else if (extension == ".7z" || extension == ".7zip")
            {
                using var archive = ArchiveFactory.OpenArchive(tempFile);
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory) continue;
                    var destPath = Path.Combine(targetFolder, Path.GetFileName(entry.Key));
                    using var entryStream = entry.OpenEntryStream();
                    using var fileStream = File.Create(destPath);
                    entryStream.CopyTo(fileStream);
                    installedFiles.Add(Path.GetRelativePath(GamePath, destPath));
                }
            }
            else
            {
                var destFile = Path.Combine(targetFolder, fileName);
                File.Copy(tempFile, destFile, true);
                installedFiles.Add(Path.GetRelativePath(GamePath, destFile));
                await ShowMessage("Warning",
                    $"Unknown file type {extension} for {mod.Name}. Copied as-is to {targetFolder}.");
            }

            File.Delete(tempFile);
            MarkModInstalled(mod, installedFiles);
            await ShowMessage("Success", $"Installed {mod.Name}");
        }
        catch (Exception ex)
        {
            await ShowMessage("Install Failed", $"Error installing {mod.Name}: {ex.Message}");
        }
    }

    private async Task UninstallMod(Mod? mod)
    {
        if (mod == null) return;
        if (!_settings.InstalledModFiles.TryGetValue(mod.Name, out var files))
        {
            await ShowMessage("Error", $"No installation record found for {mod.Name}.");
            return;
        }

        var confirm = await ShowConfirmDialog("Confirm Uninstall", $"Are you sure you want to uninstall {mod.Name}?");
        if (!confirm) return;

        try
        {
            foreach (var relativePath in files)
            {
                var fullPath = Path.Combine(GamePath, relativePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            _settings.InstalledModFiles.Remove(mod.Name);
            _settings.Save();

            _installedModNames.Remove(mod.Name);
            mod.IsInstalled = false;

            await ShowMessage("Success", $"{mod.Name} has been uninstalled.");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Uninstall failed: {ex.Message}");
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

    private bool IsMelonLoaderInstalled()
    {
        if (string.IsNullOrEmpty(GamePath)) return false;
        return File.Exists(Path.Combine(GamePath, "version.dll")) ||
               File.Exists(Path.Combine(GamePath, "version.dylib")) ||
               Directory.Exists(Path.Combine(GamePath, "MelonLoader"));
    }

    private async Task InstallMelonLoader()
    {
        if (string.IsNullOrEmpty(GamePath))
        {
            await ShowMessage("Game Path Required",
                "Please select or confirm your The Long Dark installation folder first.");
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
            string melonLoaderZipUrl =
                "https://github.com/LavaGang/MelonLoader/releases/download/v0.7.2/MelonLoader.zip";

            using var client = new HttpClient();
            var response = await client.GetAsync(melonLoaderZipUrl);
            response.EnsureSuccessStatusCode();

            var tempZip = Path.Combine(Path.GetTempPath(), "MelonLoader.zip");
            using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }

            using var archive = ZipFile.OpenRead(tempZip);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

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

    private async Task CheckForUpdates()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TLD-Mod-Manager");
            
            var response = await client.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await ShowMessage("Update Check",
                    $"Repository '{GitHubRepo}' not found. Please check:\n" +
                    "- The repository name is correct\n" +
                    "- The repository is public\n" +
                    "- You have an internet connection");
                return;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json);

            if (releases == null || releases.Count == 0)
            {
                await ShowMessage("Update Check", "No releases found in this repository.");
                return;
            }

            var latest = releases.First();
            string latestVersion = latest.TagName?.TrimStart('v', 'V') ?? "0.0.0";
            string currentVersion = CurrentVersion.TrimStart('v', 'V');

            if (latestVersion != currentVersion)
            {
                bool answer = await ShowConfirmDialog("Update Available",
                    $"A new version ({latest.TagName}) is available.\n\n" +
                    $"Current version: v{currentVersion}\n" +
                    $"New version: {latest.TagName}\n\n" +
                    "Do you want to download it now?");

                if (answer)
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = latest.HtmlUrl,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
            }
            else
            {
                await ShowMessage("Up to Date", $"You have the latest version (v{currentVersion}).");
            }
        }
        catch (HttpRequestException ex)
        {
            await ShowMessage("Update Check Failed", $"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            await ShowMessage("Update Check Failed", $"Error parsing response: {ex.Message}");
        }
        catch (Exception ex)
        {
            await ShowMessage("Update Check Failed", $"Unexpected error: {ex.Message}");
        }
    }
    
    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }

        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }

        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
    }
}