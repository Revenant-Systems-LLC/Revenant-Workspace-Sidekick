using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-001: Dangerous dynamic code execution (eval, exec, compile).</summary>
public sealed partial class DangerousEvalRule : IRule
{
    [GeneratedRegex(@"\b(eval|exec|compile)\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex EvalRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-001",
        Title: "Dangerous dynamic code execution",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in EvalRegex().Matches(context.Content))
        {
            var funcName = match.Groups[1].Value;
            var arg = match.Groups[2].Value.Trim();

            // Empty or basic call
            if (string.IsNullOrEmpty(arg))
                continue;

            // Check if it looks like a simple literal string
            bool isLiteral = (arg.StartsWith("\"\"\"") && arg.EndsWith("\"\"\"")) ||
                             (arg.StartsWith("'''") && arg.EndsWith("'''")) ||
                             (arg.StartsWith("\"") && arg.EndsWith("\"")) ||
                             (arg.StartsWith("'") && arg.EndsWith("'"));

            // If it starts with f or f""" it's an interpolated string, so not a static literal
            if (arg.StartsWith("f\"") || arg.StartsWith("f'") || arg.StartsWith("f\"\"\"") || arg.StartsWith("f'''") || 
                arg.Contains(".format(") || arg.Contains(" % "))
            {
                isLiteral = false;
            }

            if (!isLiteral)
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-PY-001",
                    Title: $"Dangerous use of dynamic {funcName}()",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: $"The '{funcName}' function executes arbitrary Python code. When called with dynamic or user-influenced input, it allows remote code execution (RCE).",
                    Fix: "Avoid dynamic execution. Use static functions, safe dictionary lookups, or a structured data format like JSON."
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
