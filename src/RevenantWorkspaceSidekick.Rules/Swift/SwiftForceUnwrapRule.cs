using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Swift;

public sealed partial class SwiftForceUnwrapRule : IRule
{
    [GeneratedRegex(@"\w+!(?!=|\s*\{)", RegexOptions.Compiled)]
    private static partial Regex ForceUnwrapRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-SW-001",
        Title: "Force unwrap of optional",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".swift"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in ForceUnwrapRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Force unwrapping an optional will crash the app if the value is nil.",
                Fix: "Use if let, guard let, or nil-coalescing (??)."
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
