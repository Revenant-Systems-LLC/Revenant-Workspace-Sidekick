using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Dart;

/// <summary>RWS-DT-004: Use of print() in production code.</summary>
public sealed partial class DartPrintStatementRule : IRule
{
    [GeneratedRegex(@"\bprint\s*\(", RegexOptions.Compiled)]
    private static partial Regex PrintRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-DT-004",
        Title: "print() statement in code",
        DefaultSeverity: Severity.Low,
        FileExtensions: [".dart"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in PrintRegex().Matches(context.Content))
        {
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//"))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-DT-004",
                Title: "print() used instead of proper logging",
                Severity: Severity.Low,
                File: context.RelativePath,
                Line: line,
                Why: "print() statements are not suitable for production Flutter/Dart apps. They can leak sensitive data and are not configurable.",
                Fix: "Use a logging package (e.g., package:logger) or debugPrint() for debug-only output."
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

    private static string GetLineText(string content, int charIndex)
    {
        var start = content.LastIndexOf('\n', Math.Max(0, charIndex - 1)) + 1;
        var end = content.IndexOf('\n', charIndex);
        if (end < 0) end = content.Length;
        return content[start..end];
    }
}
