using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-017: Functions exceeding 40 lines.</summary>
public sealed partial class PythonLargeFunctionRule : IRule
{
    [GeneratedRegex(@"^( *)def\s+(\w+)\s*\(", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex FuncDefRegex();

    private const int MaxLines = 40;

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-017",
        Title: "Function is too large",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var lines = context.Content.Split('\n');
        var matches = FuncDefRegex().Matches(context.Content);

        foreach (Match match in matches)
        {
            var funcName = match.Groups[2].Value;
            var defLine = GetLineNumber(context.Content, match.Index);
            var defIndent = match.Groups[1].Value.Length;

            // Count lines in the function body
            int bodyLines = 0;
            for (int i = defLine; i < lines.Length; i++) // defLine is 1-indexed, lines is 0-indexed
            {
                var currentLine = lines[i];

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    bodyLines++;
                    continue;
                }

                // Check if we've dedented back to or past the def's level
                var currentIndent = currentLine.Length - currentLine.TrimStart().Length;
                if (currentIndent <= defIndent && i > defLine)
                    break;

                bodyLines++;
            }

            if (bodyLines > MaxLines)
            {
                yield return new Finding(
                    RuleId: "RSH-PY-017",
                    Title: $"Function '{funcName}()' is {bodyLines} lines long (max {MaxLines})",
                    Severity: Severity.Medium,
                    File: context.RelativePath,
                    Line: defLine,
                    Why: "Large functions are harder to understand, test, and debug. They usually violate the Single-Responsibility Principle.",
                    Fix: $"Break '{funcName}()' into smaller helper functions, each doing one thing well."
                );
            }
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
