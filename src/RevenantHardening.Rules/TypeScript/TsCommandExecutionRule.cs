using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-002: TS/JS dangerous shell command execution.</summary>
public sealed partial class TsCommandExecutionRule : IRule
{
    [GeneratedRegex(@"\b(exec|execSync)\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex ExecRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-002",
        Title: "Dangerous command execution",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in ExecRegex().Matches(context.Content))
        {
            var arg = match.Groups[2].Value.Trim();
            if (IsDynamic(arg))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RSH-TS-002",
                    Title: "Dangerous dynamic shell execution via child_process.exec",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Running system commands via child_process.exec() with concatenated strings or template literals creates OS Command Injection vulnerabilities.",
                    Fix: "Use 'execFile' or 'spawn' instead, passing executable parameters as a secure list of strings which avoids spawning a system shell."
                );
            }
        }
    }

    private static bool IsDynamic(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        bool isLiteral = (arg.StartsWith("'") && arg.EndsWith("'")) ||
                         (arg.StartsWith("\"") && arg.EndsWith("\"")) ||
                         (arg.StartsWith("`") && arg.EndsWith("`"));

        if (arg.Contains("${") || arg.Contains("+") || !isLiteral)
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
