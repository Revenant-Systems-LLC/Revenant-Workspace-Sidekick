using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Kotlin;

public sealed partial class KotlinSqlInjectionRule : IRule
{
    [GeneratedRegex(@"query\s*\(\s*""SELECT.*?\$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-KT-004",
        Title: "SQL Injection risk via string template",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".kt", ".kts"]
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
                Why: "Constructing SQL queries using string templates allows SQL injection.",
                Fix: "Use parameterized queries (e.g., Room parameters or ? placeholders)."
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
