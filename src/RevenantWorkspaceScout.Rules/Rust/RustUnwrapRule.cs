using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Rust;

public sealed partial class RustUnwrapRule : IRule
{
    [GeneratedRegex(@"\.unwrap\(\)", RegexOptions.Compiled)]
    private static partial Regex UnwrapRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-RS-002",
        Title: "Use of .unwrap()",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".rs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in UnwrapRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Using .unwrap() will panic the application if the value is None or Err.",
                Fix: "Use pattern matching, ? operator, or .expect() with a clear error message."
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
