using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Secrets;

/// <summary>RWS-SEC-002: Suspicious secret in resource/config file.</summary>
public sealed partial class ResourceSecretRule : IRule
{
    [GeneratedRegex(@"(?i)(password|passwd|pwd)[""'\s]*[=:]\s*[""']?[^\s""'<>{}\[\]]{6,}[""']?")]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"(?i)(connection.?string)[^""'\n]*[""'][^""'\n]*[Pp]assword=[^""'\n]{4,}[""']")]
    private static partial Regex ConnectionStringPattern();

    [GeneratedRegex(@"(?i)(client.?secret)\s*[=:>\s]+[""'][A-Za-z0-9\-._~+/]{8,}[""']")]
    private static partial Regex ClientSecretPattern();

    private static readonly (Regex Pattern, string Label)[] Patterns =
    [
        (PasswordPattern(),         "password value"),
        (ConnectionStringPattern(), "connection string with password"),
        (ClientSecretPattern(),     "client secret"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-SEC-002",
        Title: "Suspicious secret in resource/config file",
        DefaultSeverity: Severity.High,
        FileExtensions: [".resx", ".config", ".json"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (var (pattern, label) in Patterns)
        {
            foreach (Match match in pattern.Matches(context.Content))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-SEC-002",
                    Title: $"Credential-like {label} in config/resource file",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Credentials stored in config or resource files ship with the application and are trivially extractable from the binary or install directory.",
                    Fix: "Replace hardcoded credentials with environment-variable references or a secrets manager. For connection strings, use Windows Integrated Security where possible.",
                    RedactedSnippet: SecretBlinder.Blind(match.Value)
                );
            }
        }
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
