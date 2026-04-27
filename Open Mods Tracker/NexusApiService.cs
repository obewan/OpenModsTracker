using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml.Media;

namespace OpenModsTracker;

public sealed class NexusApiService
{
    private static readonly Uri BaseUri = new("https://api.nexusmods.com/");
    private static readonly Uri GraphQlUri = new("https://api.nexusmods.com/v2/graphql");

    private const string UserByNameQuery = """
query userByName($name: String!) {
  userByName(name: $name) {
    memberId
    name
    ownedModCount
  }
}
""";

    private const string ModsByUploaderQuery = """
query mods($filter: ModsFilter, $offset: Int, $count: Int, $viewUploaderHidden: Boolean) {
  mods(
    filter: $filter,
    offset: $offset,
    count: $count,
    viewUploaderHidden: $viewUploaderHidden
  ) {
    totalCount
    nodes {
      modId
      game {
        domainName
      }
    }
  }
}
""";

    public async Task<DashboardSnapshot> BuildDashboardAsync(
        string apiKey,
        IReadOnlyList<ModReference> references,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new DashboardSnapshot
            {
                ErrorMessage = "Ajoute d'abord une cle API Nexus dans les parametres.",
                RefreshedAt = DateTimeOffset.Now
            };
        }

        if (references.Count == 0)
        {
            return new DashboardSnapshot
            {
                InfoMessage = "Ajoute quelques URLs de mods dans le portfolio pour alimenter le dashboard.",
                RefreshedAt = DateTimeOffset.Now
            };
        }

        using var httpClient = CreateClient(apiKey);
        ApiRateLimit? rateLimit = null;
        UserProfile? user = null;
        var errors = new List<string>();

        try
        {
            var validateResult = await ValidateUserAsync(httpClient, cancellationToken);
            user = validateResult.User;
            rateLimit = validateResult.RateLimit;
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        var mods = new List<PortfolioMod>();
        using var throttler = new SemaphoreSlim(4);
        var tasks = references.Select(async reference =>
        {
            await throttler.WaitAsync(cancellationToken);
            try
            {
                var result = await GetModAsync(httpClient, reference, cancellationToken);
                lock (mods)
                {
                    mods.Add(result.Mod);
                }

                if (result.RateLimit is not null)
                {
                    rateLimit = result.RateLimit;
                }
            }
            catch (Exception ex)
            {
                lock (errors)
                {
                    errors.Add($"{reference.GameDomain}/{reference.ModId}: {ex.Message}");
                }
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks);

        var orderedMods = mods
            .OrderByDescending(static mod => mod.TotalDownloads)
            .ThenByDescending(static mod => mod.Endorsements)
            .ThenBy(static mod => mod.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new DashboardSnapshot
        {
            User = user,
            RateLimit = rateLimit,
            Mods = orderedMods,
            SummaryCards = BuildSummaryCards(orderedMods),
            TopDownloads = BuildBars(orderedMods, static mod => mod.TotalDownloads, static mod => mod.DisplayDownloads, "downloads"),
            TopEndorsements = BuildBars(orderedMods, static mod => mod.Endorsements, static mod => mod.DisplayEndorsements, "endorsements"),
            RefreshedAt = DateTimeOffset.Now,
            InfoMessage = orderedMods.Count == 0 && errors.Count == 0
                ? "Aucune donnee retournee par l'API pour les mods saisis."
                : string.Empty,
            ErrorMessage = string.Join(Environment.NewLine, errors.Distinct())
        };
    }

    public async Task<(UserProfile User, ApiRateLimit? RateLimit)> ValidateUserAsync(string apiKey, CancellationToken cancellationToken)
    {
        using var httpClient = CreateClient(apiKey);
        return await ValidateUserAsync(httpClient, cancellationToken);
    }

    public async Task<ImportedPortfolio> ImportPortfolioAsync(string apiKey, string profileInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Ajoute d'abord une cle API Nexus valide.");
        }

        var normalizedName = NormalizeProfileInput(profileInput);
        using var httpClient = CreateClient(apiKey);

        var (profileName, memberId, ownedModCount) = await LookupUserByNameAsync(httpClient, normalizedName, cancellationToken);
        var references = new List<ModReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const int pageSize = 100;
        int? totalCount = null;

        for (var offset = 0; offset < Math.Max(ownedModCount, pageSize); offset += pageSize)
        {
            var page = await QueryGraphQlAsync(
                httpClient,
                ModsByUploaderQuery,
                new
                {
                    filter = new
                    {
                        uploaderId = new[]
                        {
                            new
                            {
                                value = memberId.ToString(CultureInfo.InvariantCulture)
                            }
                        }
                    },
                    offset,
                    count = pageSize,
                    viewUploaderHidden = true
                },
                cancellationToken);

            var modsNode = page.GetProperty("mods");
            totalCount ??= modsNode.GetProperty("totalCount").GetInt32();

            foreach (var modElement in modsNode.GetProperty("nodes").EnumerateArray())
            {
                if (!modElement.TryGetProperty("modId", out var modIdElement) ||
                    !modElement.TryGetProperty("game", out var gameElement) ||
                    !TryGetProperty(gameElement, "domainName", out var domainElement))
                {
                    continue;
                }

                var domainName = domainElement.GetString();
                if (string.IsNullOrWhiteSpace(domainName))
                {
                    continue;
                }

                var modId = modIdElement.GetInt64();
                var reference = new ModReference(domainName, modId, $"{domainName}|{modId}");
                if (seen.Add($"{reference.GameDomain}:{reference.ModId}"))
                {
                    references.Add(reference);
                }
            }

            if (references.Count >= totalCount || modsNode.GetProperty("nodes").GetArrayLength() == 0)
            {
                break;
            }
        }

        return new ImportedPortfolio
        {
            ProfileName = profileName,
            MemberId = memberId,
            TotalCount = totalCount ?? references.Count,
            Mods = references
                .OrderBy(static mod => mod.GameDomain, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static mod => mod.ModId)
                .ToList()
        };
    }

    private static async Task<(UserProfile User, ApiRateLimit? RateLimit)> ValidateUserAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("v1/users/validate.json", cancellationToken);
        var rateLimit = ParseRateLimit(response);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapData(document.RootElement);

        var user = new UserProfile
        {
            UserId = GetInt64(root, "user_id", "uid", "member_id", "id"),
            Name = GetString(root, "name", "username", "member_name"),
            Email = GetString(root, "email"),
            IsPremium = GetBoolean(root, "is_premium", "premium"),
            IsSupporter = GetBoolean(root, "is_supporter", "supporter")
        };

        return (user, rateLimit);
    }

    private static async Task<(string ProfileName, long MemberId, int OwnedModCount)> LookupUserByNameAsync(
        HttpClient httpClient,
        string userName,
        CancellationToken cancellationToken)
    {
        var data = await QueryGraphQlAsync(httpClient, UserByNameQuery, new { name = userName }, cancellationToken);
        var userNode = data.GetProperty("userByName");

        if (userNode.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException("Profil Nexus introuvable via GraphQL.");
        }

        return
        (
            GetString(userNode, "name"),
            GetInt64(userNode, "memberId"),
            (int)GetInt64(userNode, "ownedModCount")
        );
    }

    private static async Task<JsonElement> QueryGraphQlAsync(
        HttpClient httpClient,
        string query,
        object variables,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { query, variables }),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (TryGetProperty(document.RootElement, "errors", out var errorsElement) &&
            errorsElement.ValueKind == JsonValueKind.Array &&
            errorsElement.GetArrayLength() > 0)
        {
            var firstError = errorsElement[0];
            throw new InvalidOperationException(GetString(firstError, "message"));
        }

        return document.RootElement.GetProperty("data").Clone();
    }

    private static string NormalizeProfileInput(string profileInput)
    {
        if (string.IsNullOrWhiteSpace(profileInput))
        {
            throw new InvalidOperationException("Renseigne un nom de profil Nexus ou une URL de profil.");
        }

        var raw = profileInput.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absoluteUri))
        {
            var segments = absoluteUri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var profileIndex = Array.FindIndex(segments, static segment => segment.Equals("profile", StringComparison.OrdinalIgnoreCase));
            if (profileIndex >= 0 && profileIndex + 1 < segments.Length)
            {
                return Uri.UnescapeDataString(segments[profileIndex + 1]);
            }

            throw new InvalidOperationException("URL de profil Nexus non reconnue.");
        }

        return raw;
    }

    private static async Task<(PortfolioMod Mod, ApiRateLimit? RateLimit)> GetModAsync(
        HttpClient httpClient,
        ModReference reference,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"v1/games/{reference.GameDomain}/mods/{reference.ModId}.json",
            cancellationToken);

        var rateLimit = ParseRateLimit(response);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapData(document.RootElement);

        var mod = new PortfolioMod
        {
            Reference = reference,
            Name = GetString(root, "name", "mod_name"),
            Summary = GetString(root, "summary", "description"),
            Author = GetString(root, "author", "author_name", "uploaded_by", "mod_author", "submitted_by"),
            Version = GetString(root, "version", "mod_version"),
            Status = GetString(root, "status"),
            PictureUrl = GetString(root, "picture_url", "image_url", "mod_image", "thumbnail_url"),
            PageUrl = reference.CanonicalUrl,
            TotalDownloads = GetInt64(root, "mod_downloads", "downloads", "total_downloads", "download_count"),
            UniqueDownloads = GetInt64(root, "mod_unique_downloads", "unique_downloads", "unique_download_count"),
            Endorsements = GetInt64(root, "mod_endorsements", "endorsements", "endorsement_count"),
            UploadedAt = GetDateTime(root, "created_time", "uploaded_timestamp", "published_timestamp", "created_at"),
            UpdatedAt = GetDateTime(root, "updated_time", "updated_timestamp", "last_updated", "updated_at")
        };

        return (mod, rateLimit);
    }

    private static HttpClient CreateClient(string apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Add("apikey", apiKey);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("OpenModsTracker/2.0"));
        client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("(Windows)"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static IReadOnlyList<SummaryCard> BuildSummaryCards(IReadOnlyList<PortfolioMod> mods)
    {
        if (mods.Count == 0)
        {
            return [];
        }

        var totalDownloads = mods.Sum(static mod => mod.TotalDownloads);
        var totalUniqueDownloads = mods.Sum(static mod => mod.UniqueDownloads);
        var totalEndorsements = mods.Sum(static mod => mod.Endorsements);
        var bestDownload = mods.MaxBy(static mod => mod.TotalDownloads);
        var bestEndorsements = mods.MaxBy(static mod => mod.Endorsements);
        var lastUpdated = mods
            .Where(static mod => mod.UpdatedAt.HasValue)
            .OrderByDescending(static mod => mod.UpdatedAt)
            .FirstOrDefault();

        return
        [
            new SummaryCard
            {
                Title = "Downloads",
                Value = FormatCompact(totalDownloads),
                AccentBrush = CreateBrush("#F97316"),
                Subtitle = bestDownload is null ? Localizer.GetString("SummaryCard_PortfolioEmpty") : string.Format(Localizer.GetString("SummaryCard_Leader"), bestDownload.DisplayName)
            },
            new SummaryCard
            {
                Title = "Unique DLs",
                Value = FormatCompact(totalUniqueDownloads),
                AccentBrush = CreateBrush("#22C55E"),
                Subtitle = string.Format(Localizer.GetString("SummaryCard_TrackedMods"), mods.Count)
            },
            new SummaryCard
            {
                Title = "Endorsements",
                Value = FormatCompact(totalEndorsements),
                AccentBrush = CreateBrush("#38BDF8"),
                Subtitle = bestEndorsements is null ? Localizer.GetString("SummaryCard_NoData") : string.Format(Localizer.GetString("SummaryCard_TopSocial"), bestEndorsements.DisplayName)
            },
            new SummaryCard
            {
                Title = "Mods suivis",
                Value = FormatCompact(mods.Count),
                AccentBrush = CreateBrush("#A78BFA"),
                Subtitle = lastUpdated is null ? Localizer.GetString("SummaryCard_UnknownLastUpdate") : string.Format(Localizer.GetString("SummaryCard_LastUpdate"), lastUpdated.DisplayName)
            }
        ];
    }

    private static IReadOnlyList<MetricBarItem> BuildBars(
        IReadOnlyList<PortfolioMod> mods,
        Func<PortfolioMod, long> selector,
        Func<PortfolioMod, string> valueTextSelector,
        string subtitle)
    {
        var ranked = mods
            .OrderByDescending(selector)
            .Take(5)
            .ToList();

        if (ranked.Count == 0)
        {
            return [];
        }

        var max = Math.Max(1, ranked.Max(selector));
        return ranked
            .Select(mod => new MetricBarItem
            {
                Label = mod.DisplayName,
                ValueText = valueTextSelector(mod),
                Subtitle = $"{mod.Reference.GameDomain} - {subtitle}",
                Ratio = selector(mod) / (double)max
            })
            .ToList();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.ReasonPhrase ?? "Erreur API";
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
            {
                message = $"{message}: {content}";
            }
        }
        catch
        {
            // Ignore content parsing errors and keep the status message.
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static JsonElement UnwrapData(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            TryGetProperty(element, "data", out var dataElement) &&
            dataElement.ValueKind == JsonValueKind.Object)
        {
            return dataElement;
        }

        return element;
    }

    private static ApiRateLimit? ParseRateLimit(HttpResponseMessage response)
    {
        int? ParseInt(string header)
        {
            if (response.Headers.TryGetValues(header, out var values) &&
                int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return null;
        }

        DateTimeOffset? ParseDate(string header)
        {
            if (response.Headers.TryGetValues(header, out var values) &&
                DateTimeOffset.TryParse(values.FirstOrDefault(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
            {
                return value;
            }

            return null;
        }

        return new ApiRateLimit
        {
            DailyLimit = ParseInt("X-RL-Daily-Limit"),
            DailyRemaining = ParseInt("X-RL-Daily-Remaining"),
            DailyReset = ParseDate("X-RL-Daily-Reset"),
            HourlyLimit = ParseInt("X-RL-Hourly-Limit"),
            HourlyRemaining = ParseInt("X-RL-Hourly-Remaining"),
            HourlyReset = ParseDate("X-RL-Hourly-Reset")
        };
    }

    private static string GetString(JsonElement element, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (TryGetProperty(element, alias, out var property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString() ?? string.Empty;
                }

                if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    return property.ToString();
                }
            }
        }

        return string.Empty;
    }

    private static long GetInt64(JsonElement element, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (TryGetProperty(element, alias, out var property))
            {
                switch (property.ValueKind)
                {
                    case JsonValueKind.Number when property.TryGetInt64(out var intValue):
                        return intValue;
                    case JsonValueKind.String when long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                        return parsed;
                }
            }
        }

        return 0;
    }

    private static bool GetBoolean(JsonElement element, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (TryGetProperty(element, alias, out var property))
            {
                switch (property.ValueKind)
                {
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed):
                        return parsed;
                }
            }
        }

        return false;
    }

    private static DateTimeOffset? GetDateTime(JsonElement element, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (!TryGetProperty(element, alias, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var textValue))
            {
                return textValue;
            }

            if (property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt64(out var unixValue))
            {
                return unixValue > 10_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixValue)
                    : DateTimeOffset.FromUnixTimeSeconds(unixValue);
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string alias, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

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

    private static SolidColorBrush CreateBrush(string color)
    {
        var hex = color.TrimStart('#');
        if (hex.Length == 6)
        {
            return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
                0xFF,
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16)));
        }

        if (hex.Length == 8)
        {
            return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16)));
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
}
