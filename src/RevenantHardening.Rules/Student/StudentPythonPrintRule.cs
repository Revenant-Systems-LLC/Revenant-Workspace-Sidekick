using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Student;

/// <summary>RSH-EDU-PY-01: Detects leftover print() statements in Python code.</summary>
public sealed partial class StudentPythonPrintRule : IRule
{
    [GeneratedRegex(@"^\s*print\s*\(", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex PrintRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-EDU-PY-01",
        Title: "Leftover print() statement detected",
        DefaultSeverity: Severity.Info,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in PrintRegex().Matches(context.Content))
        {
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Using print() for debugging is fine in development, but in production AI engineering, you should use structured logging. Print statements clutter stdout and are hard to filter.",
                Fix: "Remove the print statement or replace it with the Python 'logging' module (e.g., logging.info() or logging.debug())."
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
