using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace TLD_Mod_Manager;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    public MainWindowViewModel()
    {
        ShowDetailsCommand = new AsyncCommand<Mod>(ShowDetails);
        InstallCommand = new AsyncCommand<Mod>(InstallMod);
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

        if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            await ShowMessage("Error", "Cannot determine main window.");
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Select installation folder (Mods directory of The Long Dark)"
        };
        var folder = await dialog.ShowAsync(desktop.MainWindow);
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(mod.DownloadUrl);
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(new Uri(mod.DownloadUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{mod.Name}.dll";
            var savePath = Path.Combine(folder, fileName);

            using var fileStream = File.Create(savePath);
            await response.Content.CopyToAsync(fileStream);

            await ShowMessage("Success", $"Downloaded {mod.Name} to {savePath}");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Download failed: {ex.Message}");
        }
    }
    
    private async Task ShowMessage(string title, string message)
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard(title, message);
        await msgBox.ShowAsync();
    }
}