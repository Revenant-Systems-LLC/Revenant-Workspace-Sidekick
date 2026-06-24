using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Go;

public sealed partial class GoSqlInjectionRule : IRule
{
    [GeneratedRegex(@"fmt\.Sprintf\s*\(\s*""SELECT.*?%s.*?[""'],\s*\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-GO-004",
        Title: "SQL Injection risk via fmt.Sprintf",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".go"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SqlInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Constructing SQL queries using string formatting with user input allows SQL injection.",
                Fix: "Use parameterized queries or prepared statements."
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
