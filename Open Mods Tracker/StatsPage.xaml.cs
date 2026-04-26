using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace OpenModsTracker;

public sealed partial class StatsPage : Page, INotifyPropertyChanged
{
    private string _infoMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private ISeries[] _chartSeries = [];
    private Axis[] _xAxes = [];
    private Axis[] _yAxes = [];
    private string _chartTitle = string.Empty;
    private int _selectedMetricIndex;
    private int _selectedModeIndex;
    private IReadOnlyList<string> _domains = [];
    private string _selectedDomain = "All";

    private HistoryRecord? _history;

    public StatsPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += StatsPage_Loaded;

        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(181, 192, 208))
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ISeries[] ChartSeries
    {
        get => _chartSeries;
        set => SetProperty(ref _chartSeries, value);
    }

    public Axis[] XAxes
    {
        get => _xAxes;
        set => SetProperty(ref _xAxes, value);
    }

    public Axis[] YAxes
    {
        get => _yAxes;
        set => SetProperty(ref _yAxes, value);
    }

    public string ChartTitle
    {
        get => _chartTitle;
        set => SetProperty(ref _chartTitle, value);
    }

    public int SelectedMetricIndex
    {
        get => _selectedMetricIndex;
        set => SetProperty(ref _selectedMetricIndex, value);
    }

    public int SelectedModeIndex
    {
        get => _selectedModeIndex;
        set => SetProperty(ref _selectedModeIndex, value);
    }

    public IReadOnlyList<string> Domains
    {
        get => _domains;
        set => SetProperty(ref _domains, value);
    }

    public string SelectedDomain
    {
        get => _selectedDomain;
        set
        {
            if (SetProperty(ref _selectedDomain, value))
            {
                UpdateChart();
            }
        }
    }

    private async void StatsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;

            _history = await StatsTracker.Instance.LoadHistoryAsync();
            if (_history.DataPoints.Count == 0)
            {
                InfoMessage = Localizer.GetString("StatsPage_NoHistory");
                ChartSeries = [];
                ChartTitle = Localizer.GetString("StatsPage_ChartTitle_History");
                return;
            }

            XAxes = new[]
            {
                new Axis
                {
                    Labels = _history.DataPoints.Select(x => x.Timestamp.ToString("d MMM")).ToArray(),
                    LabelsPaint = new SolidColorPaint(new SKColor(181, 192, 208))
                }
            };

            Domains = ["All", .. _history.DataPoints
                .SelectMany(p => p.Mods.Keys)
                .Select(static key => key.Split(':', 2)[0])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)];

            if (!Domains.Contains(SelectedDomain, StringComparer.OrdinalIgnoreCase))
            {
                SelectedDomain = "All";
            }

            UpdateChart();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void UpdateChart()
    {
        if (_history is null || _history.DataPoints.Count == 0)
        {
            ChartSeries = [];
            return;
        }

        var metric = SelectedMetricIndex switch
        {
            1 => Metric.Endorsements,
            _ => Metric.Downloads
        };

        var mode = SelectedModeIndex switch
        {
            1 => Mode.TopMods,
            _ => Mode.Totals
        };

        var metricLabel = metric switch
        {
            Metric.Endorsements => Localizer.GetString("StatsPage_Metric_Endorsements"),
            _ => Localizer.GetString("StatsPage_Metric_Downloads")
        };

        ChartTitle = mode == Mode.Totals
            ? $"{Localizer.GetString("StatsPage_ModeTotalsLabel")} • {metricLabel}"
            : $"{Localizer.GetString("StatsPage_ModeTop10Label")} • {metricLabel}";

        ChartSeries = mode == Mode.Totals
            ? BuildTotalsSeries(metric)
            : BuildTopModsSeries(metric);
    }

    private ISeries[] BuildTotalsSeries(Metric metric)
    {
        long[] values = metric switch
        {
            Metric.Endorsements => _history!.DataPoints.Select(p => p.TotalEndorsements).ToArray(),
            _ => _history!.DataPoints.Select(p => p.TotalDownloads).ToArray()
        };

        var color = metric switch
        {
            Metric.Endorsements => SKColors.MediumSeaGreen,
            _ => SKColors.DodgerBlue
        };

        return new ISeries[]
        {
            new LineSeries<long>
            {
                Values = values,
                Name = metric.ToString(),
                Fill = new SolidColorPaint(color.WithAlpha(45)),
                Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(color) { StrokeThickness = 2 }
            }
        };
    }

    private ISeries[] BuildTopModsSeries(Metric metric)
    {
        IEnumerable<string> sourceKeys = _history!.DataPoints
            .SelectMany(p => p.Mods.Keys);

        if (!string.IsNullOrWhiteSpace(SelectedDomain) &&
            !SelectedDomain.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            sourceKeys = sourceKeys.Where(k => k.StartsWith(SelectedDomain + ":", StringComparison.OrdinalIgnoreCase));
        }

        var keys = sourceKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (keys.Length == 0)
        {
            InfoMessage = Localizer.GetString("StatsPage_NoPerModHistory");
            return [];
        }

        // Keep it readable (top 10) even if older points had different top lists.
        keys = keys.Take(10).ToArray();

        var palette = new[]
        {
            SKColors.DodgerBlue,
            SKColors.MediumSeaGreen,
            SKColors.Orange,
            SKColors.MediumPurple,
            SKColors.Goldenrod,
            SKColors.HotPink,
            SKColors.Turquoise,
            SKColors.Coral,
            SKColors.YellowGreen,
            SKColors.SlateBlue
        };

        var result = new List<ISeries>();
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var color = palette[i % palette.Length];
            var label = _history.DataPoints
                .Select(p => p.Mods.TryGetValue(key, out var mm) ? mm.Label : null)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                ?? key;

            long[] values = _history.DataPoints.Select(p =>
            {
                if (!p.Mods.TryGetValue(key, out var mm)) return 0L;
                return metric switch
                {
                    Metric.Endorsements => mm.Endorsements,
                    _ => mm.Downloads
                };
            }).ToArray();

            result.Add(new LineSeries<long>
            {
                Values = values,
                Name = label,
                Fill = null,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
                GeometrySize = 7,
                GeometryStroke = new SolidColorPaint(color) { StrokeThickness = 2 }
            });
        }

        return result.ToArray();
    }

    private void MetricCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateChart();
    }

    private void ModeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateChart();
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

    private enum Metric
    {
        Downloads,
        Endorsements
    }

    private enum Mode
    {
        Totals,
        TopMods
    }
}

