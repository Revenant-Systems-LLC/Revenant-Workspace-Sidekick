using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Secrets;

/// <summary>RWS-SEC-004: Hardcoded connection string with embedded credentials in C# source.</summary>
public sealed partial class ConnectionStringInCodeRule : IRule
{
    // ADO.NET-style: contains Server=/Data Source= AND Password=<value>
    // Allow semicolons in the middle (they separate connection string parameters)
    [GeneratedRegex(@"(?i)(Server|Data Source|Initial Catalog)[^""]{0,300}Password\s*=[^;""'\s]{4,}")]
    private static partial Regex AdoNetConnStr();

    // MongoDB URI with embedded credentials (plain and +srv variants)
    [GeneratedRegex(@"(?i)mongodb(\+srv)?://[^:]+:[^@]{4,}@")]
    private static partial Regex MongoUri();

    // PostgreSQL/MySQL URI with embedded credentials
    [GeneratedRegex(@"(?i)(postgres|postgresql|mysql)://[^:]+:[^@]{4,}@")]
    private static partial Regex SqlUri();

    private static readonly (Regex Pattern, string Label)[] Patterns =
    [
        (AdoNetConnStr(), "ADO.NET connection string with password"),
        (MongoUri(),      "MongoDB URI with embedded credentials"),
        (SqlUri(),        "Database URI with embedded credentials"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-SEC-004",
        Title: "Hardcoded connection string in source code",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        // Only check actual string literals — not comments or disabled code
        foreach (var token in root.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.StringLiteralToken) &&
                !token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) &&
                !token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                continue;

            var value = token.ValueText;
            foreach (var (pattern, label) in Patterns)
            {
                if (!pattern.IsMatch(value))
                    continue;

                var line = token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                yield return new Finding(
                    RuleId: "RWS-SEC-004",
                    Title: $"Hardcoded {label} in C# source",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Connection strings with embedded passwords in source code are committed to version control and are trivially extractable from compiled binaries. AI assistants commonly generate these for convenience and developers forget to replace them before shipping.",
                    Fix: "Move the connection string to environment variables or a secrets manager. Use Windows Integrated Security where supported. At minimum, reference the secret via a .user file or appsettings.Development.json excluded from source control.",
                    RedactedSnippet: SecretBlinder.Blind(value)
                );
                break; // one finding per token
            }
        }
    }
}
