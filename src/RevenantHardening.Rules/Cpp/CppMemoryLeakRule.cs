using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Cpp;

/// <summary>RSH-CPP-002: Raw new/malloc without corresponding delete/free (memory leak indicator).</summary>
public sealed partial class CppMemoryLeakRule : IRule
{
    [GeneratedRegex(@"\b(new\s+\w+[\[\(]|malloc\s*\(|calloc\s*\(|realloc\s*\()", RegexOptions.Compiled)]
    private static partial Regex AllocRegex();

    [GeneratedRegex(@"\b(std::unique_ptr|std::shared_ptr|std::make_unique|std::make_shared|std::weak_ptr)\b", RegexOptions.Compiled)]
    private static partial Regex SmartPtrRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-CPP-002",
        Title: "Raw memory allocation (potential memory leak)",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cpp", ".c", ".cc", ".cxx", ".h", ".hpp"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // If the file already uses smart pointers extensively, lower the noise
        bool usesSmartPtrs = SmartPtrRegex().IsMatch(context.Content);

        foreach (Match match in AllocRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//") || lineText.TrimStart().StartsWith("*"))
                continue;

            // Skip if the allocation is assigned to a smart pointer on the same line
            if (lineText.Contains("unique_ptr") || lineText.Contains("shared_ptr"))
                continue;

            yield return new Finding(
                RuleId: "RSH-CPP-002",
                Title: "Raw memory allocation without smart pointer",
                Severity: usesSmartPtrs ? Severity.Medium : Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Raw new/malloc allocations require manual cleanup. Forgetting to delete/free causes memory leaks; double-free causes crashes.",
                Fix: "Use std::unique_ptr or std::shared_ptr (C++11+) to manage memory automatically via RAII."
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
