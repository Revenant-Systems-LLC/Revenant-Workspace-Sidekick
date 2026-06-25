using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Dart;

/// <summary>RWS-DT-003: Silent failure via empty catch block.</summary>
public sealed partial class DartSilentFailureRule : IRule
{
    [GeneratedRegex(@"catch\s*\(\s*\w+\s*\)\s*\{\s*(?://.*?\n\s*)*\}", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SilentCatchRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-DT-003",
        Title: "Silent error suppression",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".dart"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SilentCatchRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-DT-003",
                Title: "Silent failure via empty catch block",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Empty catch blocks hide errors and make debugging difficult.",
                Fix: "Log the error, rethrow it, or handle it explicitly."
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
