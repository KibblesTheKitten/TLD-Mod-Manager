using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
            FilterMods(); // Re-filter when mods change
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

    public MainWindowViewModel()
    {
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
    public AsyncCommand<Mod> ShowDetailsCommand { get; }

    public MainWindowViewModel()
    {
        ShowDetailsCommand = new AsyncCommand<Mod>(ShowDetails);
        _ = LoadModsAsync();
    }

    private async Task ShowDetails(Mod mod)
    {
        if (string.IsNullOrEmpty(mod.SourceUrl)) return;

        var service = new ModService();
        var details = await service.GetModDetailsAsync(mod.SourceUrl);
        if (details != null)
        {
            // Show a dialog or update a panel with details
            // For now, we'll just update a property and bind to it
            SelectedModDetails = details;
        }
    }