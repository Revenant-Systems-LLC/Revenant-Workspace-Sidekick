using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Core.Reporters;
using RevenantWorkspaceSidekick.Rules;
using RevenantWorkspaceSidekick.Ui.Infrastructure;

namespace RevenantWorkspaceSidekick.Ui.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private string _scanPath = "";
    private bool _studentMode;
    private string _verdictText = "";
    private bool _isWhyExpanded = true;
    private bool _isScanning;
    // Empty, not "Ready." The status line has nothing to say before a scan, and a
    // label announcing that it is idle is noise sitting in the footer permanently.
    private string _statusText = "";
    private int _score;
    private char _grade = '-';
    private int _filesScanned;
    private TimeSpan _duration;
    private FindingRowViewModel? _selectedFinding;
    private ScanResult? _lastResult;

    /// <summary>
    /// Every finding the last scan returned. <see cref="Findings"/> is what the grid
    /// shows and may be a severity-filtered subset of this, so the severity counts and
    /// the card numerals read from here — otherwise filtering to LOW would zero out
    /// the other three cards.
    /// </summary>
    private readonly List<FindingRowViewModel> _allFindings = [];

    /// <summary>Which severity card is acting as the grid filter; null means show all.</summary>
    private Severity? _severityFilter;

    public MainViewModel()
    {
        ScanCommand = new RelayCommand(RunScanAsync, () => !IsScanning && Directory.Exists(ScanPath));
        BrowseCommand = new RelayCommand(Browse, () => !IsScanning);
        ExportJsonCommand = new RelayCommand(() => Export(new JsonReporter(), "JSON files (*.json)|*.json", ".json"), () => !IsScanning && _lastResult is not null);
        ExportHtmlCommand = new RelayCommand(() => Export(new HtmlReporter(), "HTML files (*.html)|*.html", ".html"), () => !IsScanning && _lastResult is not null);

        // One command per card rather than one parameterised command, because
        // RelayCommand ignores its parameter. Four one-liners beat changing it.
        FilterLowCommand      = new RelayCommand(() => ToggleFilter(Severity.Low));
        FilterMediumCommand   = new RelayCommand(() => ToggleFilter(Severity.Medium));
        FilterHighCommand     = new RelayCommand(() => ToggleFilter(Severity.High));
        FilterCriticalCommand = new RelayCommand(() => ToggleFilter(Severity.Critical));

        ToggleWhyCommand = new RelayCommand(() => IsWhyExpanded = !IsWhyExpanded);
    }

    /// <summary>What the grid displays. Filtered view of <see cref="_allFindings"/>.</summary>
    public ObservableCollection<FindingRowViewModel> Findings { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];

    public string ScanPath
    {
        get => _scanPath;
        set
        {
            if (SetField(ref _scanPath, value))
                ScanCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// The closing sentence on a finished scan. Roast wording arrives on its own
    /// when the grade earns it — there is no roast switch. Empty until a scan runs.
    /// </summary>
    public string VerdictText
    {
        get => _verdictText;
        private set
        {
            if (SetField(ref _verdictText, value))
                OnPropertyChanged(nameof(HasVerdict));
        }
    }

    public bool HasVerdict => !string.IsNullOrEmpty(VerdictText);

    // ── Why This Matters tray ────────────────────────────────────────────────
    // The middle column collapses to a vertical tab so the findings list can take
    // the whole body when you are scanning down a list rather than reading one
    // finding. Column width is a bindable GridLength rather than a converter:
    // star while open, Auto while closed so the tab's own width defines it.

    public bool IsWhyExpanded
    {
        get => _isWhyExpanded;
        set
        {
            if (SetField(ref _isWhyExpanded, value))
            {
                OnPropertyChanged(nameof(IsWhyCollapsed));
                OnPropertyChanged(nameof(WhyColumnWidth));
            }
        }
    }

    public bool IsWhyCollapsed => !IsWhyExpanded;

    public GridLength WhyColumnWidth =>
        IsWhyExpanded ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;

    /// <summary>Verdict color, matching the grade ring so the two agree.</summary>
    public Brush VerdictBrush => SeverityBrushes.ForGrade(Grade);

    public bool StudentMode
    {
        get => _studentMode;
        set => SetField(ref _studentMode, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set => SetField(ref _isScanning, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public int Score
    {
        get => _score;
        private set => SetField(ref _score, value);
    }

    public char Grade
    {
        get => _grade;
        private set
        {
            if (SetField(ref _grade, value))
            {
                OnPropertyChanged(nameof(GradeText));
                OnPropertyChanged(nameof(GradeBrush));
                OnPropertyChanged(nameof(HasGrade));
                OnPropertyChanged(nameof(VerdictBrush));
            }
        }
    }

    public string GradeText => Grade.ToString();

    /// <summary>
    /// False until a scan produces a grade. The ring shows the Vegvisir mark while
    /// this is false and the grade letter once it is true — so the empty state is
    /// the company mark rather than a placeholder dash.
    /// </summary>
    public bool HasGrade => Grade != '-';

    /// <summary>
    /// Grade ring color. Delegates to <see cref="SeverityBrushes.ForGrade"/> so the
    /// on-screen grade matches the exported HTML report exactly.
    /// </summary>
    public Brush GradeBrush => SeverityBrushes.ForGrade(Grade);

    public int FilesScanned
    {
        get => _filesScanned;
        private set => SetField(ref _filesScanned, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        private set
        {
            if (SetField(ref _duration, value))
                OnPropertyChanged(nameof(DurationText));
        }
    }

    public string DurationText => Duration.ToString(@"mm\:ss\.ff");

    public FindingRowViewModel? SelectedFinding
    {
        get => _selectedFinding;
        set => SetField(ref _selectedFinding, value);
    }

    public int CriticalCount => _allFindings.Count(f => f.Severity == Severity.Critical);
    public int HighCount => _allFindings.Count(f => f.Severity == Severity.High);
    public int MediumCount => _allFindings.Count(f => f.Severity == Severity.Medium);
    public int LowCount => _allFindings.Count(f => f.Severity == Severity.Low);

    // ── Severity cards as filters ────────────────────────────────────────────
    // Each card paints itself from these. A card that IS the active filter fills
    // with its 35% brush; when some other card holds the filter, this one fades
    // back so the active one is unmistakable. No filter, everything at full.

    public Brush LowCardBackground => CardBackground(Severity.Low);
    public Brush MediumCardBackground => CardBackground(Severity.Medium);
    public Brush HighCardBackground => CardBackground(Severity.High);
    public Brush CriticalCardBackground => CardBackground(Severity.Critical);

    public double LowCardOpacity => CardOpacity(Severity.Low);
    public double MediumCardOpacity => CardOpacity(Severity.Medium);
    public double HighCardOpacity => CardOpacity(Severity.High);
    public double CriticalCardOpacity => CardOpacity(Severity.Critical);

    private Brush CardBackground(Severity severity) =>
        _severityFilter == severity ? SeverityBrushes.ActiveFor(severity) : SeverityBrushes.WashFor(severity);

    private double CardOpacity(Severity severity) =>
        _severityFilter is null || _severityFilter == severity ? 1.0 : 0.55;

    public RelayCommand ScanCommand { get; }
    public RelayCommand BrowseCommand { get; }
    public RelayCommand ExportJsonCommand { get; }
    public RelayCommand ExportHtmlCommand { get; }
    public RelayCommand FilterLowCommand { get; }
    public RelayCommand FilterMediumCommand { get; }
    public RelayCommand FilterHighCommand { get; }
    public RelayCommand FilterCriticalCommand { get; }
    public RelayCommand ToggleWhyCommand { get; }

    /// <summary>
    /// Click a severity card to show only that severity; click the same card again
    /// to clear. Clicking a different card moves the filter rather than stacking.
    /// </summary>
    private void ToggleFilter(Severity severity)
    {
        _severityFilter = _severityFilter == severity ? null : severity;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Findings.Clear();
        foreach (var finding in _allFindings)
        {
            if (_severityFilter is null || finding.Severity == _severityFilter)
                Findings.Add(finding);
        }

        OnPropertyChanged(nameof(LowCardBackground));
        OnPropertyChanged(nameof(MediumCardBackground));
        OnPropertyChanged(nameof(HighCardBackground));
        OnPropertyChanged(nameof(CriticalCardBackground));
        OnPropertyChanged(nameof(LowCardOpacity));
        OnPropertyChanged(nameof(MediumCardOpacity));
        OnPropertyChanged(nameof(HighCardOpacity));
        OnPropertyChanged(nameof(CriticalCardOpacity));

        if (_lastResult is not null)
            StatusText = DescribeCurrentView();
    }

    /// <summary>
    /// Status line for a completed scan. Says how much of the result the grid is
    /// currently showing, so a filtered view can never be mistaken for a clean one.
    /// </summary>
    private string DescribeCurrentView()
    {
        var suppressed = _lastResult?.SuppressedCount ?? 0;
        var tail = suppressed > 0 ? $", {suppressed} suppressed" : "";

        return _severityFilter is { } severity
            ? $"Showing {Findings.Count} of {_allFindings.Count} finding(s) — {severity.ToString().ToUpperInvariant()} only{tail}. Click the card again to clear."
            : $"Scan complete — {_allFindings.Count} finding(s){tail}.";
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a project folder to scan",
        };

        if (dialog.ShowDialog() == true)
        {
            ScanPath = dialog.FolderName;
        }
    }

    private async Task RunScanAsync()
    {
        if (!Directory.Exists(ScanPath))
        {
            StatusText = $"Path does not exist: {ScanPath}";
            return;
        }

        IsScanning = true;
        StatusText = "Scanning...";
        Warnings.Clear();
        Findings.Clear();
        _allFindings.Clear();

        // A filter from the previous scan would silently hide most of the new one.
        _severityFilter = null;

        VerdictText = "";

        // Always scan at the floor. There used to be a MIN SEVERITY dropdown here,
        // exposing the CLI's --severity flag. In a window that control is a trap:
        // it filters what the ENGINE reports, and Score and Grade are computed from
        // whatever survives it — so raising the floor silently inflates your grade.
        // The severity cards filter the view instead, which leaves the score honest.
        // Student mode still drops the floor to Info, matching ScanCommand.
        var floor = StudentMode ? Severity.Info : Severity.Low;

        // Roast is decided after the fact now, from the grade, so it is passed false
        // here. It never did anything at this layer regardless — ConsoleReporter was
        // the only thing that ever read it, and the UI does not use that reporter.
        var options = new ScanOptions(
            ScanPath, "console", null, false, false, floor,
            [], [], StudentMode);

        try
        {
            var result = await Task.Run(() => RuleEngine.Scan(RuleRegistry.All, options));
            _lastResult = result;

            // RuleEngine.Scan already filters by MinSeverity and computes Score/Grade
            // from that filtered set, so no need to filter again here.
            foreach (var finding in result.Findings)
                _allFindings.Add(new FindingRowViewModel(finding));

            Score = result.Score;
            Grade = result.Grade;
            FilesScanned = result.FilesScanned;
            Duration = result.Duration;

            // The roast shows up on its own at D or F. There is no switch to arm —
            // an opinion you had to opt into before seeing the result was never
            // going to land, and by the time a project grades D it has earned it.
            VerdictText = Verdict.For(result.Grade, Verdict.EarnsRoast(result.Grade), StudentMode);

            // Fills Findings from _allFindings, repaints the cards, sets StatusText.
            ApplyFilter();

            OnPropertyChanged(nameof(CriticalCount));
            OnPropertyChanged(nameof(HighCount));
            OnPropertyChanged(nameof(MediumCount));
            OnPropertyChanged(nameof(LowCount));
        }
        catch (Exception ex)
        {
            Warnings.Add($"Scan failed: {ex.Message}");
            StatusText = "Scan failed — see warnings.";
        }
        finally
        {
            IsScanning = false;
            ExportJsonCommand.RaiseCanExecuteChanged();
            ExportHtmlCommand.RaiseCanExecuteChanged();
        }
    }

    private void Export(IReporter reporter, string filter, string defaultExt)
    {
        if (_lastResult is null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = $"rws-report{defaultExt}",
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var writer = new StreamWriter(dialog.FileName, append: false);
            reporter.Report(_lastResult, writer);
            StatusText = $"Exported to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            Warnings.Add($"Export failed: {ex.Message}");
        }
    }
}
