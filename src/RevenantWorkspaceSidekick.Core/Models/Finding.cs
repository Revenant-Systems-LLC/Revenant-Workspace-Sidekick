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
    string? RedactedSnippet = null,
    bool? Verified = null,       // null = not tested, true = confirmed live, false = dead
    bool FromHistory = false,    // true = finding is from git history, not current HEAD
    [property: System.Text.Json.Serialization.JsonIgnore] string? RawValue = null  // ephemeral; never serialized
);
