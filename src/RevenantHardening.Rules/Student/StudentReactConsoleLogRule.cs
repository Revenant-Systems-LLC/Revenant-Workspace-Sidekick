using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Student;

/// <summary>RSH-EDU-FE-01: Detects console.log in TS/JS/React.</summary>
public sealed partial class StudentReactConsoleLogRule : IRule
{
    [GeneratedRegex(@"console\.log\s*\(", RegexOptions.Compiled)]
    private static partial Regex ConsoleLogRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-EDU-FE-01",
        Title: "Leftover console.log() statement",
        DefaultSeverity: Severity.Info,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in ConsoleLogRegex().Matches(context.Content))
        {
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//"))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Console logs leak internal data to end users and look unprofessional in production builds.",
                Fix: "Remove the console.log() call, or use a proper logger that can be disabled in production."
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
