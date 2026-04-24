using System.Text.Json;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace OpenModsTracker;

internal sealed class AppController
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static AppController? _instance;
    public static AppController Instance => _instance ??= new AppController();

    private readonly NexusApiService _apiService = new();
    private readonly SemaphoreSlim _dashboardLock = new(1, 1);
    private readonly SemaphoreSlim _settingsLock = new(1, 1);
    private DesktopAcrylicBackdrop? _desktopAcrylicBackdrop;
    private DashboardSnapshot? _snapshot;
    private DateTimeOffset _snapshotTimestamp;
    private AppSettings? _settingsCache;

    private const string SettingsFolderName = "OpenModsTracker";
    private const string SettingsFileName = "settings.json";

    private AppController()
    {
    }

    public bool TrySetDesktopAcrylicBackdrop(Window window)
    {
        if (!DesktopAcrylicController.IsSupported())
        {
            return false;
        }

        _desktopAcrylicBackdrop = new DesktopAcrylicBackdrop();
        window.SystemBackdrop = _desktopAcrylicBackdrop;
        return true;
    }

    public void SaveThemePreference(ApplicationTheme theme)
    {
        var settings = LoadSettings();
        settings.ApplicationTheme = theme;
        SaveSettings(settings);
    }

    public ApplicationTheme? LoadThemePreference()
    {
        return LoadSettings().ApplicationTheme;
    }

    public void SaveUserKey(string key)
    {
        var settings = LoadSettings();
        settings.UserKey = key ?? string.Empty;
        SaveSettings(settings);
        InvalidateDashboard();
    }

    public string LoadUserKey()
    {
        return LoadSettings().UserKey;
    }

    public void SavePortfolio(string input)
    {
        var settings = LoadSettings();
        settings.Portfolio = input ?? string.Empty;
        SaveSettings(settings);
        InvalidateDashboard();
    }

    public string LoadPortfolio()
    {
        return LoadSettings().Portfolio;
    }

    public IReadOnlyList<ModReference> LoadPortfolioReferences()
    {
        return ModReference.ParseMany(LoadPortfolio());
    }

    public async Task<UserProfile> ValidateUserAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = LoadUserKey();
        return (await _apiService.ValidateUserAsync(apiKey, cancellationToken)).User;
    }

    public DashboardSnapshot? GetCachedDashboard()
    {
        return _snapshot;
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await _dashboardLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh &&
                _snapshot is not null &&
                DateTimeOffset.Now - _snapshotTimestamp < TimeSpan.FromMinutes(5))
            {
                return _snapshot;
            }

            _snapshot = await _apiService.BuildDashboardAsync(LoadUserKey(), LoadPortfolioReferences(), cancellationToken);
            _snapshotTimestamp = DateTimeOffset.Now;
            return _snapshot;
        }
        finally
        {
            _dashboardLock.Release();
        }
    }

    public void InvalidateDashboard()
    {
        _snapshot = null;
        _snapshotTimestamp = default;
    }

    private AppSettings LoadSettings()
    {
        _settingsLock.Wait();
        try
        {
            if (_settingsCache is not null)
            {
                return _settingsCache;
            }

            var filePath = GetSettingsFilePath();
            if (!File.Exists(filePath))
            {
                _settingsCache = new AppSettings();
                return _settingsCache;
            }

            var raw = File.ReadAllText(filePath);
            _settingsCache = JsonSerializer.Deserialize<AppSettings>(raw, SerializerOptions) ?? new AppSettings();
            return _settingsCache;
        }
        catch
        {
            _settingsCache = new AppSettings();
            return _settingsCache;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    private void SaveSettings(AppSettings settings)
    {
        _settingsLock.Wait();
        try
        {
            var directory = GetSettingsDirectoryPath();
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, SettingsFileName);
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, SerializerOptions));
            _settingsCache = settings;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    private static string GetSettingsDirectoryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SettingsFolderName);
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(GetSettingsDirectoryPath(), SettingsFileName);
    }

    private sealed class AppSettings
    {
        public ApplicationTheme? ApplicationTheme { get; set; }
        public string UserKey { get; set; } = string.Empty;
        public string Portfolio { get; set; } = string.Empty;
    }
}
