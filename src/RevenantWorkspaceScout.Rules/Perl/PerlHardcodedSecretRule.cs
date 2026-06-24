using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Perl;

public sealed partial class PerlHardcodedSecretRule : IRule
{
    [GeneratedRegex(@"(?i)\$(password|secret|api_key|token)\s*=\s*[""'][^""']+[""']", RegexOptions.Compiled)]
    private static partial Regex HardcodedSecretRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PL-002",
        Title: "Hardcoded secret or credential",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".pl", ".pm"]
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
                Why: "Hardcoding credentials in scripts poses a critical security risk.",
                Fix: "Load secrets from environment variables (e.g., $ENV{'SECRET'})."
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
