using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Kotlin;

public sealed partial class KotlinSilentFailureRule : IRule
{
    [GeneratedRegex(@"catch\s*\([^)]+\)\s*\{\s*\}", RegexOptions.Compiled)]
    private static partial Regex SilentFailureRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-KT-002",
        Title: "Silent error suppression",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".kt", ".kts"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SilentFailureRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Empty catch blocks hide errors.",
                Fix: "Log the exception or handle it."
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
