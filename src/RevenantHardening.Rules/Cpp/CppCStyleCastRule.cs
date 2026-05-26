using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Cpp;

/// <summary>RSH-CPP-003: Use of C-style casts instead of C++ casts.</summary>
public sealed partial class CppCStyleCastRule : IRule
{
    // Match C-style casts like (int)x, (char*)ptr, (void*)x — but not inside comments
    [GeneratedRegex(@"\(\s*(unsigned\s+)?(int|char|float|double|long|short|void|size_t|uint\d+_t|int\d+_t)\s*\*?\s*\)\s*\w", RegexOptions.Compiled)]
    private static partial Regex CStyleCastRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-CPP-003",
        Title: "C-style cast used instead of C++ cast",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".cpp", ".cc", ".cxx", ".hpp"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in CStyleCastRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//") || lineText.TrimStart().StartsWith("*"))
                continue;

            yield return new Finding(
                RuleId: "RSH-CPP-003",
                Title: "C-style cast — prefer static_cast/reinterpret_cast",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "C-style casts bypass the type system and are hard to search for. They can silently perform dangerous reinterpretations.",
                Fix: "Use static_cast<T>(), dynamic_cast<T>(), or reinterpret_cast<T>() for explicit, searchable, and safer conversions."
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
