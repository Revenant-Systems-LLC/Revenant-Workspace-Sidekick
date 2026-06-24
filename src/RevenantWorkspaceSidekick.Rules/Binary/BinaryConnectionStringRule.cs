using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Binary;

/// <summary>
/// RWS-BIN-002: Connection string with embedded credentials found in compiled binary strings.
/// </summary>
public sealed partial class BinaryConnectionStringRule : IRule
{
    [GeneratedRegex(@"(?i)(Server|Data Source|Initial Catalog)[^""]{0,300}Password\s*=[^;""'\s]{4,}")]
    private static partial Regex AdoNetConnStr();

    [GeneratedRegex(@"(?i)mongodb(\+srv)?://[^:]+:[^@]{4,}@")]
    private static partial Regex MongoUri();

    [GeneratedRegex(@"(?i)(postgres|postgresql|mysql)://[^:]+:[^@]{4,}@")]
    private static partial Regex SqlUri();

    private static readonly (Regex Pattern, string Label)[] Patterns =
    [
        (AdoNetConnStr(), "ADO.NET connection string with password"),
        (MongoUri(),      "MongoDB URI with credentials"),
        (SqlUri(),        "Database URI with credentials"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-BIN-002",
        Title: "Hardcoded connection string found in compiled binary",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".dll", ".exe"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        if (string.IsNullOrEmpty(context.Content))
            yield break;

        foreach (var (pattern, label) in Patterns)
        {
            if (!pattern.IsMatch(context.Content))
                continue;

            yield return new Finding(
                RuleId: "RWS-BIN-002",
                Title: $"Hardcoded {label} in compiled binary",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: null,
                Why: "Connection strings with embedded passwords in a published binary allow anyone with the binary to extract database credentials using a hex editor or strings tool. Rotate the credential and all sessions it may have established.",
                Fix: "Remove the connection string from source. Rotate the database password immediately. Use environment variables, Windows DPAPI, or a secrets manager. Rebuild and republish."
            );
        }
    }
}
