using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Go;

/// <summary>RWS-GO-003: Silent failure via ignored errors.</summary>
public sealed partial class GoSilentFailureRule : IRule
{
    // Match 'if err != nil {' followed by only whitespace, comments, or nothing before '}'
    [GeneratedRegex(@"if\s+err\s*!=\s*nil\s*\{\s*(//[^\n]*)?\s*\}", RegexOptions.Compiled)]
    private static partial Regex EmptyErrCheckRegex();

    // Match '_ = someFunc(...)' which discards return values (typically errors)
    [GeneratedRegex(@"_\s*=\s*\w+[\w.]*\s*\(", RegexOptions.Compiled)]
    private static partial Regex DiscardedErrorRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-GO-003",
        Title: "Silent failure via ignored error",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".go"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // 1. Empty or comment-only err != nil blocks
        foreach (Match match in EmptyErrCheckRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-GO-003",
                Title: "Empty error handling block",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Checking for an error but not handling it silently swallows failures, making debugging and monitoring impossible.",
                Fix: "Log the error, return it, or handle it explicitly. At minimum use log.Printf(\"error: %v\", err)."
            );
        }

        // 2. Discarded errors via _ = someFunc()
        foreach (Match match in DiscardedErrorRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-GO-003",
                Title: "Discarded error return value",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Assigning a function's error return to '_' explicitly discards it, hiding potential failures.",
                Fix: "Capture the error and handle it: if err := someFunc(); err != nil { log.Fatal(err) }."
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
