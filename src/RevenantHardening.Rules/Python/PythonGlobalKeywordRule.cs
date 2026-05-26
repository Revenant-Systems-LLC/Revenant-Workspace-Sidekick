using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-013: Use of the 'global' keyword.</summary>
public sealed partial class PythonGlobalKeywordRule : IRule
{
    [GeneratedRegex(@"^\s*global\s+\w+", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex GlobalRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-013",
        Title: "Use of the 'global' keyword",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in GlobalRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-013",
                Title: "Use of the 'global' keyword",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "The 'global' keyword breaks encapsulation and makes functions dependent on external mutable state. Most CS professors and style guides forbid it.",
                Fix: "Pass the variable as a function parameter and return the updated value instead."
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
