using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace OpenModsTracker;

public sealed partial class HomePage : Page, INotifyPropertyChanged
{
    private bool _isBusy;
    private string _infoMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private string _userDisplayName = "Compte inconnu";
    private string _userTier = "Standard";
    private string _refreshedAt = "Jamais";
    private string _dailyQuota = "n/a";
    private string _hourlyQuota = "n/a";
    private IReadOnlyList<SummaryCard> _summaryCards = [];
    private IReadOnlyList<MetricBarItem> _topDownloads = [];
    private IReadOnlyList<MetricBarItem> _topEndorsements = [];
    private IReadOnlyList<PortfolioMod> _spotlightMods = [];
    private string _modsCountText = "0 mod";

    public HomePage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += HomePage_Loaded;
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
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string InfoMessage
    {
        get => _infoMessage;
        set
        {
            if (SetProperty(ref _infoMessage, value))
            {
                OnPropertyChanged(nameof(HasInfoMessage));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrors));
            }
        }
    }

    public string UserDisplayName
    {
        get => _userDisplayName;
        set => SetProperty(ref _userDisplayName, value);
    }

    public string UserTier
    {
        get => _userTier;
        set => SetProperty(ref _userTier, value);
    }

    public string RefreshedAt
    {
        get => _refreshedAt;
        set => SetProperty(ref _refreshedAt, value);
    }

    public string DailyQuota
    {
        get => _dailyQuota;
        set => SetProperty(ref _dailyQuota, value);
    }

    public string HourlyQuota
    {
        get => _hourlyQuota;
        set => SetProperty(ref _hourlyQuota, value);
    }

    public IReadOnlyList<SummaryCard> SummaryCards
    {
        get => _summaryCards;
        set => SetProperty(ref _summaryCards, value);
    }

    public IReadOnlyList<MetricBarItem> TopDownloads
    {
        get => _topDownloads;
        set => SetProperty(ref _topDownloads, value);
    }

    public IReadOnlyList<MetricBarItem> TopEndorsements
    {
        get => _topEndorsements;
        set => SetProperty(ref _topEndorsements, value);
    }

    public IReadOnlyList<PortfolioMod> SpotlightMods
    {
        get => _spotlightMods;
        set => SetProperty(ref _spotlightMods, value);
    }

    public string ModsCountText
    {
        get => _modsCountText;
        set => SetProperty(ref _modsCountText, value);
    }

    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync(false);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync(true);
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

    private async Task LoadDashboardAsync(bool forceRefresh)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = await AppController.Instance.GetDashboardAsync(forceRefresh);
            UserDisplayName = snapshot.User?.DisplayName ?? "Compte non valide";
            UserTier = snapshot.User?.DisplayTier ?? "Standard";
            RefreshedAt = snapshot.DisplayRefreshedAt;
            DailyQuota = snapshot.RateLimit?.DisplayDaily ?? "n/a";
            HourlyQuota = snapshot.RateLimit?.DisplayHourly ?? "n/a";
            SummaryCards = snapshot.SummaryCards;
            TopDownloads = snapshot.TopDownloads;
            TopEndorsements = snapshot.TopEndorsements;
            SpotlightMods = snapshot.Mods.Take(6).ToList();
            ModsCountText = snapshot.Mods.Count switch
            {
                <= 1 => $"{snapshot.Mods.Count} mod",
                _ => $"{snapshot.Mods.Count} mods"
            };
            InfoMessage = snapshot.InfoMessage;
            ErrorMessage = snapshot.ErrorMessage;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
