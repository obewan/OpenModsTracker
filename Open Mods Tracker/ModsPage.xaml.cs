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
    private IReadOnlyList<PortfolioMod> _mods = [];
    private string _collectionSummary = "0 mod";

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
            Mods = snapshot.Mods;
            CollectionSummary = snapshot.Mods.Count switch
            {
                <= 1 => $"{snapshot.Mods.Count} mod",
                _ => $"{snapshot.Mods.Count} mods"
            };
            StatusMessage = string.IsNullOrWhiteSpace(snapshot.ErrorMessage)
                ? snapshot.InfoMessage
                : snapshot.ErrorMessage;
        }
        finally
        {
            IsBusy = false;
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
