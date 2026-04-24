using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Media;

namespace OpenModsTracker;

public sealed record ModReference(string GameDomain, long ModId, string Source)
{
    private static readonly Regex NexusUrlRegex =
        new(@"nexusmods\.com/(?<game>[^/\s]+)/mods/(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string CanonicalUrl => $"https://www.nexusmods.com/{GameDomain}/mods/{ModId}";

    public static IReadOnlyList<ModReference> ParseMany(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var results = new List<ModReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParse(rawLine, out var reference))
            {
                var parsedReference = reference!;
                var key = $"{parsedReference.GameDomain}:{parsedReference.ModId}";
                if (seen.Add(key))
                {
                    results.Add(parsedReference);
                }
            }
        }

        return results;
    }

    public static bool TryParse(string? raw, out ModReference? reference)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var value = raw.Trim();
        var urlMatch = NexusUrlRegex.Match(value);
        if (urlMatch.Success &&
            long.TryParse(urlMatch.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var urlModId))
        {
            reference = new ModReference(urlMatch.Groups["game"].Value.ToLowerInvariant(), urlModId, value);
            return true;
        }

        foreach (var separator in new[] { '|', ';', ',' })
        {
            var parts = value.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var splitModId))
            {
                reference = new ModReference(parts[0].ToLowerInvariant(), splitModId, value);
                return true;
            }
        }

        return false;
    }
}

public sealed class PortfolioMod
{
    public required ModReference Reference { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PictureUrl { get; init; } = string.Empty;
    public string PageUrl { get; init; } = string.Empty;
    public long TotalDownloads { get; init; }
    public long UniqueDownloads { get; init; }
    public long Endorsements { get; init; }
    public long Views { get; init; }
    public long Comments { get; init; }
    public long Bugs { get; init; }
    public long Tracking { get; init; }
    public DateTimeOffset? UploadedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"{Reference.GameDomain}/{Reference.ModId}" : Name;
    public string DisplayAuthor => string.IsNullOrWhiteSpace(Author) ? "Auteur inconnu" : Author;
    public string DisplaySummary => string.IsNullOrWhiteSpace(Summary) ? "Aucun resume fourni par l'API." : Summary;
    public string DisplayVersion => string.IsNullOrWhiteSpace(Version) ? "Version inconnue" : $"v{Version}";
    public string DisplayStatus => string.IsNullOrWhiteSpace(Status) ? "Statut indisponible" : Status.Replace('_', ' ');
    public string DisplayUpdatedAt => UpdatedAt?.ToLocalTime().ToString("dd MMM yyyy", CultureInfo.CurrentCulture) ?? "n/a";
    public string DisplayUploadedAt => UploadedAt?.ToLocalTime().ToString("dd MMM yyyy", CultureInfo.CurrentCulture) ?? "n/a";
    public string DisplayDownloads => FormatCompact(TotalDownloads);
    public string DisplayUniqueDownloads => FormatCompact(UniqueDownloads);
    public string DisplayEndorsements => FormatCompact(Endorsements);
    public string DisplayViews => FormatCompact(Views);
    public string DisplayComments => FormatCompact(Comments);
    public string DisplayBugs => FormatCompact(Bugs);
    public string DisplayTracking => FormatCompact(Tracking);
    public string DownloadsLabel => $"DL {DisplayDownloads}";
    public string EndorsementsLabel => $"Endorsements {DisplayEndorsements}";
    public string ViewsLabel => $"Views {DisplayViews}";
    public string UpdatedLabel => $"Maj {DisplayUpdatedAt}";
    public string AuthorLabel => $"Auteur: {DisplayAuthor}";
    public string UpdatedAtLabel => $"Maj: {DisplayUpdatedAt}";
    public string UploadedAtLabel => $"Creation: {DisplayUploadedAt}";

    private static string FormatCompact(long value)
    {
        return value switch
        {
            >= 1_000_000_000 => $"{value / 1_000_000_000d:0.0}B",
            >= 1_000_000 => $"{value / 1_000_000d:0.0}M",
            >= 1_000 => $"{value / 1_000d:0.0}K",
            _ => value.ToString("N0", CultureInfo.CurrentCulture)
        };
    }
}

public sealed class SummaryCard
{
    public required string Title { get; init; }
    public required string Value { get; init; }
    public required Brush AccentBrush { get; init; }
    public required string Subtitle { get; init; }
}

public sealed class MetricBarItem
{
    public required string Label { get; init; }
    public required string ValueText { get; init; }
    public required string Subtitle { get; init; }
    public required double Ratio { get; init; }
}

public sealed class UserProfile
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public long UserId { get; init; }
    public bool IsPremium { get; init; }
    public bool IsSupporter { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Utilisateur Nexus" : Name;
    public string DisplayTier => IsPremium ? "Premium" : IsSupporter ? "Supporter" : "Standard";
}

public sealed class ApiRateLimit
{
    public int? DailyRemaining { get; init; }
    public int? DailyLimit { get; init; }
    public int? HourlyRemaining { get; init; }
    public int? HourlyLimit { get; init; }
    public DateTimeOffset? DailyReset { get; init; }
    public DateTimeOffset? HourlyReset { get; init; }

    public string DisplayDaily => DailyRemaining.HasValue && DailyLimit.HasValue
        ? $"{DailyRemaining.Value:N0} / {DailyLimit.Value:N0}"
        : "n/a";

    public string DisplayHourly => HourlyRemaining.HasValue && HourlyLimit.HasValue
        ? $"{HourlyRemaining.Value:N0} / {HourlyLimit.Value:N0}"
        : "n/a";
}

public sealed class DashboardSnapshot
{
    public UserProfile? User { get; init; }
    public ApiRateLimit? RateLimit { get; init; }
    public IReadOnlyList<PortfolioMod> Mods { get; init; } = [];
    public IReadOnlyList<SummaryCard> SummaryCards { get; init; } = [];
    public IReadOnlyList<MetricBarItem> TopDownloads { get; init; } = [];
    public IReadOnlyList<MetricBarItem> TopEndorsements { get; init; } = [];
    public DateTimeOffset RefreshedAt { get; init; }
    public string InfoMessage { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;

    public bool HasMods => Mods.Count > 0;
    public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string DisplayRefreshedAt => RefreshedAt == default
        ? "Jamais"
        : RefreshedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
}

public sealed class ImportedPortfolio
{
    public required string ProfileName { get; init; }
    public required long MemberId { get; init; }
    public required int TotalCount { get; init; }
    public required IReadOnlyList<ModReference> Mods { get; init; }
}
