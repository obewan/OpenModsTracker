using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenModsTracker;

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    private string _statusMessage = string.Empty;
    private string _validationMessage = ""; // Initialized in Loaded or not needed if placeholder is fine
    private string _portfolioSummary = Localizer.GetString("SettingsPage_ModsZero");
    private string _importSummary = ""; // Can be set if needed

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += SettingsPage_Loaded;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    public string PortfolioSummary
    {
        get => _portfolioSummary;
        set => SetProperty(ref _portfolioSummary, value);
    }

    public string ImportSummary
    {
        get => _importSummary;
        set => SetProperty(ref _importSummary, value);
    }

    public bool CanValidate => !string.IsNullOrWhiteSpace(ApiKeyPasswordBox?.Password);

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApiKeyPasswordBox.Password = AppController.Instance.LoadUserKey();
        PortfolioTextBox.Text = AppController.Instance.LoadPortfolio();
        UpdatePortfolioSummary();
        OnPropertyChanged(nameof(CanValidate));

        var cachedDashboard = AppController.Instance.GetCachedDashboard();
        if (cachedDashboard?.User is not null)
        {
            ImportProfileTextBox.Text = cachedDashboard.User.Name;
        }

        var lang = AppController.Instance.LoadLanguagePreference();
        if (string.IsNullOrEmpty(lang)) lang = "en-US";
        
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag.ToString() == lang)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            AppController.Instance.SaveLanguagePreference(lang);
            StatusMessage = Localizer.GetString("SettingsPage_LangSaved");
        }
    }

    private void OnThemeSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        ApplicationTheme applicationTheme = themeToggleSwitch.IsOn ? ApplicationTheme.Light : ApplicationTheme.Dark;
        AppController.Instance.SaveThemePreference(applicationTheme);
        StatusMessage = Localizer.GetString("SettingsPage_ThemeSaved");
    }

    private void OnThemeSwitch_Loaded(object sender, RoutedEventArgs e)
    {
        ApplicationTheme? theme = AppController.Instance.LoadThemePreference();
        themeToggleSwitch.IsOn = theme.HasValue
            ? theme.Value == ApplicationTheme.Light
            : App.Current.RequestedTheme == ApplicationTheme.Light;
    }

    private void RevealModeCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        ApiKeyPasswordBox.PasswordRevealMode = RevealCheckBox.IsChecked == true
            ? PasswordRevealMode.Visible
            : PasswordRevealMode.Hidden;
    }

    private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(nameof(CanValidate));
    }

    private void PortfolioTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePortfolioSummary();
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAll();
        StatusMessage = Localizer.GetString("SettingsPage_ConfigSaved");
    }

    private async void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveAll();
            var user = await AppController.Instance.ValidateUserAsync();
            ValidationMessage = $"{Localizer.GetString("SettingsPage_ValidationSuccess")} {user.DisplayName} ({user.DisplayTier}).";
            StatusMessage = Localizer.GetString("SettingsPage_ValidationSuccess");
            if (string.IsNullOrWhiteSpace(ImportProfileTextBox.Text))
            {
                ImportProfileTextBox.Text = user.Name;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"{Localizer.GetString("SettingsPage_ValidationFailed")}{ex.Message}";
        }
    }

    private async void SaveAndRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveAll();
            await AppController.Instance.GetDashboardAsync(true);
            StatusMessage = Localizer.GetString("SettingsPage_ConfigSavedRefreshed");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async void ImportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveAll();
            ImportSummary = Localizer.GetString("SettingsPage_ImportInProgress");
            var importedPortfolio = await new NexusApiService().ImportPortfolioAsync(
                ApiKeyPasswordBox.Password,
                ImportProfileTextBox.Text,
                CancellationToken.None);

            var merged = MergePortfolioWith(importedPortfolio.Mods);
            PortfolioTextBox.Text = string.Join(Environment.NewLine, merged.Select(static mod => mod.CanonicalUrl));
            SaveAll();

            ImportSummary = $"{importedPortfolio.Mods.Count} mods imported for {importedPortfolio.ProfileName}";
            StatusMessage = Localizer.GetString("SettingsPage_ImportFinished");
        }
        catch (Exception ex)
        {
            ImportSummary = $"{Localizer.GetString("SettingsPage_ImportFailed")}{ex.Message}";
        }
    }

    private void SaveAll()
    {
        AppController.Instance.SaveUserKey(ApiKeyPasswordBox.Password);
        AppController.Instance.SavePortfolio(PortfolioTextBox.Text);
        UpdatePortfolioSummary();
        ValidationMessage = Localizer.GetString("SettingsPage_KeySaved");
    }

    private void UpdatePortfolioSummary()
    {
        var parsed = ModReference.ParseMany(PortfolioTextBox.Text);
        PortfolioSummary = parsed.Count switch
        {
            0 => Localizer.GetString("SettingsPage_ModsZero"),
            1 => $"{Localizer.GetString("SettingsPage_ModsOne")}{parsed[0].GameDomain}/{parsed[0].ModId}",
            _ => $"{parsed.Count}{Localizer.GetString("SettingsPage_ModsMany")}{parsed[0].GameDomain}/{parsed[0].ModId}"
        };
    }

    private IReadOnlyList<ModReference> MergePortfolioWith(IEnumerable<ModReference> importedMods)
    {
        var map = new Dictionary<string, ModReference>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in ModReference.ParseMany(PortfolioTextBox.Text))
        {
            map[$"{mod.GameDomain}:{mod.ModId}"] = mod;
        }

        foreach (var mod in importedMods)
        {
            map[$"{mod.GameDomain}:{mod.ModId}"] = mod;
        }

        return map.Values
            .OrderBy(static mod => mod.GameDomain, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static mod => mod.ModId)
            .ToList();
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
