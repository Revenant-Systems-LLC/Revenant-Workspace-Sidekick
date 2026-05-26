using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-018: Poor variable naming practices.</summary>
public sealed partial class PythonBadNamingRule : IRule
{
    // Single-letter assignments (but allow i, j, k, _ for loop counters)
    [GeneratedRegex(@"^\s*([a-hA-Hl-zL-Z])\s*=\s", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SingleLetterRegex();

    // Generic/meaningless variable names
    private static readonly HashSet<string> BadNames =
    [
        "data", "temp", "tmp", "val", "val1", "val2",
        "thing", "stuff", "foo", "bar", "baz",
        "result1", "result2", "var1", "var2",
        "aa", "bb", "cc", "dd", "ee", "ff",
        "xx", "yy", "zz", "abc"
    ];

    [GeneratedRegex(@"^\s*(\w+)\s*=\s", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex AssignmentRegex();

    // Class name not using PascalCase
    [GeneratedRegex(@"^\s*class\s+([a-z]\w*)\s*[:(]", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex LowercaseClassRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-018",
        Title: "Poor variable or class naming",
        DefaultSeverity: Severity.Low,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // Check for single-letter variable names (not i, j, k, _)
        foreach (Match match in SingleLetterRegex().Matches(context.Content))
        {
            var varName = match.Groups[1].Value;
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-018",
                Title: $"Single-letter variable name '{varName}'",
                Severity: Severity.Low,
                File: context.RelativePath,
                Line: line,
                Why: "Single-letter variable names (except loop counters i, j, k) make code hard to understand. A reader cannot tell what the variable represents.",
                Fix: $"Give '{varName}' a descriptive name that explains its purpose (e.g., 'count', 'total', 'user_name')."
            );
        }

        // Check for generic/meaningless names
        foreach (Match match in AssignmentRegex().Matches(context.Content))
        {
            var varName = match.Groups[1].Value.ToLower();
            if (!BadNames.Contains(varName))
                continue;

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-018",
                Title: $"Generic variable name '{match.Groups[1].Value}'",
                Severity: Severity.Low,
                File: context.RelativePath,
                Line: line,
                Why: $"The name '{match.Groups[1].Value}' is too generic and doesn't describe what the variable holds.",
                Fix: "Choose a name that communicates the variable's purpose (e.g., 'student_scores' instead of 'data')."
            );
        }

        // Check for lowercase class names
        foreach (Match match in LowercaseClassRegex().Matches(context.Content))
        {
            var className = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-018",
                Title: $"Class '{className}' should use PascalCase",
                Severity: Severity.Low,
                File: context.RelativePath,
                Line: line,
                Why: "PEP 8 requires class names to use PascalCase (CapitalizedWords). Lowercase class names are confusing and non-standard.",
                Fix: $"Rename '{className}' to '{char.ToUpper(className[0]) + className[1..]}'."
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
