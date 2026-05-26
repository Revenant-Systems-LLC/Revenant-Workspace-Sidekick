using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Java;

/// <summary>RSH-JV-008: Silent failure via bare catch block.</summary>
public sealed partial class JavaSilentFailureRule : IRule
{
    [GeneratedRegex(@"catch\s*\(\s*(?:Exception|Throwable)\s+\w+\s*\)\s*\{\s*(?://.*?\n\s*)*\}", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SilentFailureRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-JV-008",
        Title: "Silent error suppression",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SilentFailureRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-JV-008",
                Title: "Silent failure via empty catch block",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Catching exceptions and doing nothing hides errors, making debugging and monitoring difficult.",
                Fix: "Log the exception using a logging framework, or rethrow it as a custom application exception."
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
