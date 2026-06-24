using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Binary;

/// <summary>
/// RWS-BIN-001: API key, token, or secret pattern found in extracted strings from a compiled binary.
/// Operates on the printable-ASCII string content extracted by FileWalker for .dll/.exe files.
/// </summary>
public sealed partial class BinarySecretRule : IRule
{
    [GeneratedRegex(@"(?i)(api[_\-]?key|apikey|api[_\-]?secret)\s*[=:]\s*[A-Za-z0-9+/\-_]{20,}")]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"sk-[A-Za-z0-9]{20,}")]
    private static partial Regex OpenAiKeyPattern();

    [GeneratedRegex(@"ghp_[A-Za-z0-9]{36}")]
    private static partial Regex GithubPatPattern();

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}")]
    private static partial Regex AwsKeyPattern();

    private static readonly (Regex Pattern, string Label)[] Patterns =
    [
        (ApiKeyPattern(),    "API key/secret"),
        (OpenAiKeyPattern(), "OpenAI-style key"),
        (GithubPatPattern(), "GitHub PAT"),
        (AwsKeyPattern(),    "AWS access key"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-BIN-001",
        Title: "Hardcoded secret found in compiled binary",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".dll", ".exe"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        if (string.IsNullOrEmpty(context.Content))
            yield break;

        foreach (var (pattern, label) in Patterns)
        {
            var match = pattern.Match(context.Content);
            if (!match.Success)
                continue;

            yield return new Finding(
                RuleId: "RWS-BIN-001",
                Title: $"Hardcoded {label} found in compiled binary",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: null,
                Why: "Secrets embedded in compiled binaries are trivially extractable using strings(1) or any PE viewer. Even if removed from source control, a published binary leaks the credential to anyone who downloads or reverse-engineers the file.",
                Fix: "Remove the secret from the source code. Rotate the credential immediately. Use environment variables or a secrets manager. Rebuild and republish the binary."
            );
        }
    }
}
