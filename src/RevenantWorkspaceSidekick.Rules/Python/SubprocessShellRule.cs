using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-002: Dangerous Shell Execution / Subprocess.</summary>
public sealed partial class SubprocessShellRule : IRule
{
    [GeneratedRegex(@"\bsubprocess\.(run|Popen|call|check_call|check_output)\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex SubprocessRegex();

    [GeneratedRegex(@"\bos\.(system|popen)\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex OsSystemRegex();

    [GeneratedRegex(@"\bshell\s*=\s*True\b")]
    private static partial Regex ShellTrueRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-002",
        Title: "Dangerous shell execution",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // 1. Check subprocess calls with shell=True
        foreach (Match match in SubprocessRegex().Matches(context.Content))
        {
            var apiName = match.Groups[1].Value;
            var args = match.Groups[2].Value;

            if (ShellTrueRegex().IsMatch(args))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-PY-002",
                    Title: $"Dangerous subprocess.{apiName} with shell=True",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Running a subprocess through the system shell (shell=True) bypasses argument escaping and opens the application to shell injection vulnerabilities.",
                    Fix: "Set shell=False and pass the command and its arguments as a list of strings: subprocess.run(['command', 'arg1', 'arg2'])."
                );
            }
        }

        // 2. Check os.system / os.popen
        foreach (Match match in OsSystemRegex().Matches(context.Content))
        {
            var apiName = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-002",
                Title: $"Dangerous use of os.{apiName}()",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: $"The 'os.{apiName}' API invokes commands via the system shell, which makes it highly vulnerable to command injection if arguments are dynamic.",
                Fix: "Use the 'subprocess' module instead with shell=False (e.g. subprocess.run(['cmd', 'arg']))."
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
}
