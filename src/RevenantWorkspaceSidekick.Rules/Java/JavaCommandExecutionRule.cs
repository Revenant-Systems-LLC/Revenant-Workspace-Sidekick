using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Java;

/// <summary>RWS-JV-001: Java dynamic shell execution.</summary>
public sealed partial class JavaCommandExecutionRule : IRule
{
    [GeneratedRegex(@"\bRuntime\.getRuntime\(\)\.exec\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex RuntimeExecRegex();

    [GeneratedRegex(@"\bnew\s+ProcessBuilder\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex ProcessBuilderRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-001",
        Title: "Java dynamic command execution",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // Runtime.getRuntime().exec()
        foreach (Match match in RuntimeExecRegex().Matches(context.Content))
        {
            var arg = match.Groups[1].Value.Trim();
            if (IsDynamic(arg))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-JV-001",
                    Title: "Dangerous dynamic system command execution",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Running system commands using dynamic, unvalidated, or concatenated parameters opens the application to Command Injection vulnerabilities, allowing remote attackers to run arbitrary system shell instructions.",
                    Fix: "Avoid dynamic execution. If executing subprocesses is required, pass parameters strictly as a list/array of literal strings to avoid shell parsing: Runtime.getRuntime().exec(new String[]{\"command\", \"arg1\"})."
                );
            }
        }

        // ProcessBuilder
        foreach (Match match in ProcessBuilderRegex().Matches(context.Content))
        {
            var arg = match.Groups[1].Value.Trim();
            if (IsDynamic(arg))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-JV-001",
                    Title: "Dangerous ProcessBuilder command execution",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "ProcessBuilder executed with dynamic, dynamic-length arrays or concatenated strings can lead to OS command injection if input parameters are influenced by users.",
                    Fix: "Ensure all parameters passed to ProcessBuilder are strictly sanitized, or use static arrays of literal strings: new ProcessBuilder(\"cmd\", \"arg1\")."
                );
            }
        }
    }

    private static bool IsDynamic(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        // A static string literal in Java starts/ends with double quotes
        bool isLiteral = arg.StartsWith("\"") && arg.EndsWith("\"");

        // Concatenation or variable references make it dynamic
        if (arg.Contains("+") || arg.Contains("String.format") || !isLiteral)
            isLiteral = false;

        return !isLiteral;
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
