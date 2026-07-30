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

    public Brush SeverityBrush => SeverityBrushes.For(Severity);
    public Brush SeverityWash => SeverityBrushes.WashFor(Severity);
}
