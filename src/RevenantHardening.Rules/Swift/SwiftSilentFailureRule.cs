using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Swift;

public sealed partial class SwiftSilentFailureRule : IRule
{
    [GeneratedRegex(@"\btry\?\s+", RegexOptions.Compiled)]
    private static partial Regex TryOptionalRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-SW-002",
        Title: "Silent failure using try?",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".swift"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in TryOptionalRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Using 'try?' silently converts errors to nil, making debugging difficult.",
                Fix: "Use a do-catch block to properly handle the error."
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
