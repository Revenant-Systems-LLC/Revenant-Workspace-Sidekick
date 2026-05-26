using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-008: Silent failure via bare catch block.</summary>
public sealed partial class TypeScriptSilentFailureRule : IRule
{
    [GeneratedRegex(@"catch\s*(?:\(\s*\w+\s*\))?\s*\{\s*(?://.*?\n\s*)*\}", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SilentFailureRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-008",
        Title: "Silent error suppression",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SilentFailureRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-008",
                Title: "Silent failure via empty catch block",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Catching exceptions and doing nothing hides errors, making debugging and monitoring difficult.",
                Fix: "Log the exception or handle it explicitly. Avoid empty catch blocks."
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
