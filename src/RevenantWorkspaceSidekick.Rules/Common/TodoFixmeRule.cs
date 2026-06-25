using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Common;

/// <summary>RWS-COM-001: Detects unresolved TODO, FIXME, or XXX comments.</summary>
public sealed partial class TodoFixmeRule : IRule
{
    [GeneratedRegex(@"(?i)\b(todo|fixme|xxx)\b:?", RegexOptions.Compiled)]
    private static partial Regex TodoRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-COM-001",
        Title: "Unresolved TODO or FIXME comment",
        DefaultSeverity: Severity.Low,
        FileExtensions: [".cs", ".ts", ".js", ".py", ".java", ".c", ".cpp", ".h", ".hpp", ".go", ".rs", ".php", ".rb"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var matches = TodoRegex().Matches(context.Content);
        foreach (Match match in matches)
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-COM-001",
                Title: "Unresolved developer note",
                Severity: Severity.Low,
                File: context.RelativePath,
                Line: line,
                Why: "TODO, FIXME, and XXX comments indicate incomplete work or technical debt that should be resolved or tracked in an issue tracker.",
                Fix: "Resolve the issue mentioned in the comment, or convert it to a tracked ticket in your issue tracker."
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
