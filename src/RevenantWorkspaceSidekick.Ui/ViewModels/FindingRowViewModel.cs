using System.Windows.Media;
using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Ui.Infrastructure;

namespace RevenantWorkspaceSidekick.Ui.ViewModels;

public sealed class FindingRowViewModel(Finding finding)
{
    public Finding Finding { get; } = finding;

    public string RuleId => Finding.RuleId;
    public string Title => Finding.Title;
    public Severity Severity => Finding.Severity;
    public string File => Finding.File;
    public string Line => Finding.Line?.ToString() ?? "—";
    public string Why => Finding.Why;
    public string Fix => Finding.Fix;

    /// <summary>Uppercase severity, as it appears on the badge and in reports.</summary>
    public string SeverityLabel => Severity.ToString().ToUpperInvariant();

    /// <summary>
    /// <c>path/to/File.cs:88</c> — the muted second line under a finding's title
    /// in the grid, and the row's hover tooltip. The grid trims this line with an
    /// ellipsis when the column is narrow; the tooltip is where the whole thing is
    /// readable. <see cref="Finding.File"/> is already relative to the scan root.
    /// </summary>
    public string FileLine => Finding.Line is { } line ? $"{File}:{line}" : File;

    public Brush SeverityBrush => SeverityBrushes.For(Severity);
    public Brush SeverityWash => SeverityBrushes.WashFor(Severity);
}
