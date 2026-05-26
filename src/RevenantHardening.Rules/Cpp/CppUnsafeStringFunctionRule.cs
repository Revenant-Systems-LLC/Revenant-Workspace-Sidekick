using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Cpp;

/// <summary>RSH-CPP-001: Unsafe C string functions vulnerable to buffer overflow.</summary>
public sealed partial class CppUnsafeStringFunctionRule : IRule
{
    [GeneratedRegex(@"\b(strcpy|strcat|sprintf|gets|scanf|vsprintf|strtok)\s*\(", RegexOptions.Compiled)]
    private static partial Regex UnsafeFuncRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-CPP-001",
        Title: "Unsafe C string function (buffer overflow risk)",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cpp", ".c", ".cc", ".cxx", ".h", ".hpp"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in UnsafeFuncRegex().Matches(context.Content))
        {
            var funcName = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//") || lineText.TrimStart().StartsWith("*"))
                continue;

            yield return new Finding(
                RuleId: "RSH-CPP-001",
                Title: $"Unsafe function '{funcName}()' — buffer overflow risk",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: $"'{funcName}()' does not perform bounds checking. Writing past the end of a buffer causes undefined behavior, crashes, and is a classic exploit vector.",
                Fix: $"Use the safer alternative: strncpy, strncat, snprintf, fgets, or std::string."
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
