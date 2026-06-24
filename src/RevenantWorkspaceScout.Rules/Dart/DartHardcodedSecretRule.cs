using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Dart;

/// <summary>RWS-DT-002: Hardcoded secrets in Dart code.</summary>
public sealed partial class DartHardcodedSecretRule : IRule
{
    [GeneratedRegex(@"(?i)\b(password|secret|apiKey|token)\s*=\s*([""'])(.+?)\2", RegexOptions.Compiled)]
    private static partial Regex SecretRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-DT-002",
        Title: "Hardcoded secret or credential",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".dart"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SecretRegex().Matches(context.Content))
        {
            var varName = match.Groups[1].Value;
            var val = match.Groups[3].Value;
            if (string.IsNullOrWhiteSpace(val) || val.ToLower() == "test" || val.ToLower() == "dummy")
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-DT-002",
                Title: $"Hardcoded {varName}",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoding credentials in source code can lead to security breaches if the code is exposed.",
                Fix: "Use environment variables, --dart-define, or a .env file loaded at runtime."
            );
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
