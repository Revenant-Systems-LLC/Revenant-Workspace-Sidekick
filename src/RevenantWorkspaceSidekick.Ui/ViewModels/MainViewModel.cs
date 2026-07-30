using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private Severity _minSeverity = Severity.Low;
    private bool _roast;
    private bool _studentMode;
    private bool _isScanning;
    private string _statusText = "Ready.";
    private int _score;
    private char _grade = '-';
    private int _filesScanned;
    private TimeSpan _duration;
    private FindingRowViewModel? _selectedFinding;
    private ScanResult? _lastResult;

    public MainViewModel()
    {
        ScanCommand = new RelayCommand(RunScanAsync, () => !IsScanning && Directory.Exists(ScanPath));
        BrowseCommand = new RelayCommand(Browse, () => !IsScanning);
        ExportJsonCommand = new RelayCommand(() => Export(new JsonReporter(), "JSON files (*.json)|*.json", ".json"), () => !IsScanning && _lastResult is not null);
        ExportHtmlCommand = new RelayCommand(() => Export(new HtmlReporter(), "HTML files (*.html)|*.html", ".html"), () => !IsScanning && _lastResult is not null);
    }

    public ObservableCollection<FindingRowViewModel> Findings { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];
    public IEnumerable<Severity> SeverityLevels { get; } = Enum.GetValues<Severity>();

    public string ScanPath
    {
        get => _scanPath;
        set
        {
            if (SetField(ref _scanPath, value))
                ScanCommand.RaiseCanExecuteChanged();
        }
    }

    public Severity MinSeverity
    {
        get => _minSeverity;
        set => SetField(ref _minSeverity, value);
    }

    public bool Roast
    {
        get => _roast;
        set => SetField(ref _roast, value);
    }

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
            }
        }
    }

    public string GradeText => Grade.ToString();

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

    public int CriticalCount => Findings.Count(f => f.Severity == Severity.Critical);
    public int HighCount => Findings.Count(f => f.Severity == Severity.High);
    public int MediumCount => Findings.Count(f => f.Severity == Severity.Medium);
    public int LowCount => Findings.Count(f => f.Severity == Severity.Low);

    public RelayCommand ScanCommand { get; }
    public RelayCommand BrowseCommand { get; }
    public RelayCommand ExportJsonCommand { get; }
    public RelayCommand ExportHtmlCommand { get; }

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

        // Student mode forces the floor to Info, mirroring ScanCommand's own CLI parsing.
        var effectiveMinSeverity = StudentMode && MinSeverity > Severity.Info ? Severity.Info : MinSeverity;

        var options = new ScanOptions(
            ScanPath, "console", null, false, Roast, effectiveMinSeverity,
            [], [], StudentMode);

        try
        {
            var result = await Task.Run(() => RuleEngine.Scan(RuleRegistry.All, options));
            _lastResult = result;

            // RuleEngine.Scan already filters by MinSeverity and computes Score/Grade
            // from that filtered set, so no need to filter again here.
            foreach (var finding in result.Findings)
                Findings.Add(new FindingRowViewModel(finding));

            Score = result.Score;
            Grade = result.Grade;
            FilesScanned = result.FilesScanned;
            Duration = result.Duration;
            StatusText = result.SuppressedCount > 0
                ? $"Scan complete — {Findings.Count} finding(s), {result.SuppressedCount} suppressed."
                : $"Scan complete — {Findings.Count} finding(s).";

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
