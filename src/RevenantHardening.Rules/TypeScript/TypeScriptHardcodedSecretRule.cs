using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-006: Hardcoded secrets.</summary>
public sealed partial class TypeScriptHardcodedSecretRule : IRule
{
    [GeneratedRegex(@"(?i)\b(?:const|let|var)\s+(password|secret|api_key|token)\s*=\s*([""'])(.+?)\2", RegexOptions.Compiled)]
    private static partial Regex SecretRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-006",
        Title: "Hardcoded secret or credential",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SecretRegex().Matches(context.Content))
        {
            var varName = match.Groups[1].Value;
            var secretValue = match.Groups[3].Value;

            if (string.IsNullOrWhiteSpace(secretValue) || 
                secretValue.ToLower() == "test" || 
                secretValue.ToLower() == "dummy" ||
                secretValue.Contains("${") || 
                secretValue.StartsWith("env."))
            {
                continue;
            }

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-006",
                Title: $"Hardcoded {varName}",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoding credentials or secrets in source code can lead to security breaches if the code is exposed.",
                Fix: "Use environment variables (e.g., process.env) or a secure secret management system."
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
