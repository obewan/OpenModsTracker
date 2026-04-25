using System.Text.Json;

namespace OpenModsTracker;

public sealed class HistoryDataPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public long TotalDownloads { get; set; }
    public long TotalEndorsements { get; set; }
    public long TotalViews { get; set; }
}

public sealed class HistoryRecord
{
    public List<HistoryDataPoint> DataPoints { get; set; } = new();
}

public sealed class StatsTracker
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private const string HistoryFileName = "history.json";
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static StatsTracker? _instance;
    public static StatsTracker Instance => _instance ??= new StatsTracker();

    private StatsTracker() { }

    private static string GetHistoryFilePath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenModsTracker");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, HistoryFileName);
    }

    public async Task SaveSnapshotAsync(DashboardSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.HasMods) return;

        await _lock.WaitAsync();
        try
        {
            var history = await LoadHistoryInternalAsync();
            var now = DateTimeOffset.Now;

            // Only save one point per day to keep history clean, unless testing
            // We check if the last point is from the same day
            if (history.DataPoints.Count > 0)
            {
                var last = history.DataPoints.Last();
                if (last.Timestamp.Date == now.Date)
                {
                    // Update today's point instead of adding a new one
                    last.Timestamp = now;
                    last.TotalDownloads = snapshot.Mods.Sum(m => m.TotalDownloads);
                    last.TotalEndorsements = snapshot.Mods.Sum(m => m.Endorsements);
                    last.TotalViews = snapshot.Mods.Sum(m => m.Views);
                    await SaveHistoryInternalAsync(history);
                    return;
                }
            }

            history.DataPoints.Add(new HistoryDataPoint
            {
                Timestamp = now,
                TotalDownloads = snapshot.Mods.Sum(m => m.TotalDownloads),
                TotalEndorsements = snapshot.Mods.Sum(m => m.Endorsements),
                TotalViews = snapshot.Mods.Sum(m => m.Views)
            });

            await SaveHistoryInternalAsync(history);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<HistoryRecord> LoadHistoryAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await LoadHistoryInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<HistoryRecord> LoadHistoryInternalAsync()
    {
        var filePath = GetHistoryFilePath();
        if (!File.Exists(filePath))
            return new HistoryRecord();

        try
        {
            using var stream = File.OpenRead(filePath);
            var record = await JsonSerializer.DeserializeAsync<HistoryRecord>(stream, SerializerOptions);
            return record ?? new HistoryRecord();
        }
        catch
        {
            return new HistoryRecord();
        }
    }

    private async Task SaveHistoryInternalAsync(HistoryRecord record)
    {
        var filePath = GetHistoryFilePath();
        using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, record, SerializerOptions);
    }
}
