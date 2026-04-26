using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace OpenModsTracker;

public sealed partial class ModsPage : Page, INotifyPropertyChanged
{
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private IReadOnlyList<PortfolioMod> _rawMods = [];
    private IReadOnlyList<PortfolioMod> _mods = [];
    private string _collectionSummary = "0 mod";
    private int _selectedSortIndex = 0;
    private IReadOnlyList<string> _domains = [];
    private string _selectedDomain = "All";
    private string _searchText = string.Empty;
    private int _totalModsCount;

    public ModsPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += ModsPage_Loaded;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
            }
        }
    }

    public bool CanRefresh => !IsBusy;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public IReadOnlyList<PortfolioMod> Mods
    {
        get => _mods;
        set => SetProperty(ref _mods, value);
    }

    public string CollectionSummary
    {
        get => _collectionSummary;
        set => SetProperty(ref _collectionSummary, value);
    }

    public IReadOnlyList<string> Domains
    {
        get => _domains;
        set => SetProperty(ref _domains, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplySort();
            }
        }
    }

    public string SelectedDomain
    {
        get => _selectedDomain;
        set
        {
            if (SetProperty(ref _selectedDomain, value))
            {
                ApplySort();
            }
        }
    }

    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                ApplySort();
            }
        }
    }

    private async void ModsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync(false);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync(true);
    }

    private async void OpenModButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string rawUri &&
            Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            await Launcher.LaunchUriAsync(uri);
        }
    }

    private async Task LoadAsync(bool forceRefresh)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = await AppController.Instance.GetDashboardAsync(forceRefresh);
            _rawMods = snapshot.Mods;
            _totalModsCount = snapshot.Mods.Count;
            Domains = ["All", .. snapshot.Mods
                .Select(m => m.Reference.GameDomain)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)];

            if (!Domains.Contains(SelectedDomain, StringComparer.OrdinalIgnoreCase))
            {
                SelectedDomain = "All";
            }
            ApplySort();
            StatusMessage = string.IsNullOrWhiteSpace(snapshot.ErrorMessage)
                ? snapshot.InfoMessage
                : snapshot.ErrorMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySort()
    {
        if (_rawMods == null) return;

        IEnumerable<PortfolioMod> filtered = _rawMods;

        if (!string.IsNullOrWhiteSpace(SelectedDomain) &&
            !SelectedDomain.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(m => string.Equals(m.Reference.GameDomain, SelectedDomain, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            filtered = filtered.Where(m =>
                (!string.IsNullOrWhiteSpace(m.DisplayName) && m.DisplayName.Contains(needle, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.DisplaySummary) && m.DisplaySummary.Contains(needle, StringComparison.CurrentCultureIgnoreCase)));
        }

        var filteredList = SelectedSortIndex switch
        {
            0 => filtered.OrderByDescending(m => m.TotalDownloads).ToList(),
            1 => filtered.OrderByDescending(m => m.Endorsements).ToList(),
            _ => filtered.OrderByDescending(m => m.TotalDownloads).ToList()
        };

        Mods = filteredList;

        if (_totalModsCount <= 0)
        {
            CollectionSummary = "0 mod";
        }
        else if (filteredList.Count == _totalModsCount)
        {
            CollectionSummary = _totalModsCount <= 1 ? $"{_totalModsCount} mod" : $"{_totalModsCount} mods";
        }
        else
        {
            CollectionSummary = $"{filteredList.Count} / {_totalModsCount} mods";
        }
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
