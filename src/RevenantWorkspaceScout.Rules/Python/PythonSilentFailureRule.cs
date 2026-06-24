using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-008: Silent failure via bare except.</summary>
public sealed partial class PythonSilentFailureRule : IRule
{
    [GeneratedRegex(@"except\s*(?:Exception(?:\s*as\s+\w+)?)?\s*:\s*(?:#.*?\n\s*)*pass\b", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SilentFailureRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-008",
        Title: "Silent failure via empty except block",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SilentFailureRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-008",
                Title: "Silent error suppression",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Using 'except: pass' or catching generic Exceptions and doing nothing hides errors, making debugging and monitoring difficult.",
                Fix: "Log the exception or handle it explicitly. Avoid using bare except clauses."
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
