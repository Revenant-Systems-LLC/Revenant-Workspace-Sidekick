namespace RevenantWorkspaceSidekick.Core.Models;

public sealed record Finding(
    string RuleId,
    string Title,
    Severity Severity,
    string File,
    int? Line,
    string Why,
    string Fix,
    string? Example = null,
    string? RedactedSnippet = null
);
