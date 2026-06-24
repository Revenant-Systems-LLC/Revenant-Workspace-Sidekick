using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Go;

public sealed partial class GoHardcodedSecretRule : IRule
{
    [GeneratedRegex(@"(?i)(password|secret|apikey|token)\s*:=\s*[""'][^""']+[""']", RegexOptions.Compiled)]
    private static partial Regex HardcodedSecretRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-GO-002",
        Title: "Hardcoded secret or credential",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".go"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in HardcodedSecretRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoded secrets can be easily extracted from source code.",
                Fix: "Use environment variables or a secure secret manager."
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
