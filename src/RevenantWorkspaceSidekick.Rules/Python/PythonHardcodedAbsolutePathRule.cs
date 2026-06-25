using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-011: Hardcoded absolute file paths.</summary>
public sealed partial class PythonHardcodedAbsolutePathRule : IRule
{
    [GeneratedRegex(@"([""'])([A-Za-z]:\\[^""']+|/(?:Users|home|var|tmp|opt|etc)/[^""']+)\1", RegexOptions.Compiled)]
    private static partial Regex AbsolutePathRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-011",
        Title: "Hardcoded absolute file path",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in AbsolutePathRegex().Matches(context.Content))
        {
            var path = match.Groups[2].Value;

            // Skip common false positives: shebangs, well-known system paths in comments
            if (path == "/usr/bin/env" || path == "/usr/bin/python" || path == "/usr/bin/python3")
                continue;

            var line = GetLineNumber(context.Content, match.Index);

            // Check if this line is a comment
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            yield return new Finding(
                RuleId: "RWS-PY-011",
                Title: $"Hardcoded absolute path: {path}",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Absolute paths break portability. When a TA or grading script runs your code on a different machine, the path won't exist and the program will crash.",
                Fix: "Use relative paths (e.g., './data.csv'), os.path.join(), or pathlib.Path for cross-platform compatibility."
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
