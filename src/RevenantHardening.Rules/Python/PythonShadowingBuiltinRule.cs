using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-012: Shadowing Python built-in names.</summary>
public sealed partial class PythonShadowingBuiltinRule : IRule
{
    private static readonly HashSet<string> Builtins =
    [
        "list", "dict", "set", "str", "int", "float", "bool", "tuple", "type",
        "id", "input", "print", "len", "range", "map", "filter", "zip",
        "max", "min", "sum", "abs", "round", "sorted", "reversed",
        "open", "file", "object", "hash", "next", "iter",
        "any", "all", "dir", "vars", "help", "format"
    ];

    [GeneratedRegex(@"^(\s*)(\w+)\s*=\s", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex AssignmentRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-012",
        Title: "Shadowing a Python built-in name",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in AssignmentRegex().Matches(context.Content))
        {
            var varName = match.Groups[2].Value;

            if (!Builtins.Contains(varName))
                continue;

            // Skip if it's inside a comment line
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-012",
                Title: $"Variable '{varName}' shadows a Python built-in",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: $"Assigning to '{varName}' overwrites the built-in function/type of the same name. After this line, the original built-in is no longer accessible in this scope, which can cause subtle bugs.",
                Fix: $"Rename the variable to something descriptive (e.g., '{varName}_value', 'my_{varName}')."
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
