using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-007: SQL Injection via string concatenation.</summary>
public sealed partial class PythonSqlInjectionRule : IRule
{
    // Match basic SQL queries constructed with f-strings or string concatenation/formatting
    [GeneratedRegex(@"(?i)f[""'].*?\b(select|insert|update|delete)\b.*?(from|into|table).*?\{.+?\}.*?[""']|[""'].*?\b(select|insert|update|delete)\b.*?(from|into|table).*?[""']\s*(\+|%|\.format)", RegexOptions.Compiled)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-007",
        Title: "SQL Injection vulnerability",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SqlInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-007",
                Title: "SQL Injection via dynamic query construction",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Constructing SQL queries using string concatenation, f-strings, or formatting allows attackers to inject malicious SQL commands.",
                Fix: "Use parameterized queries or an ORM to safely execute SQL statements."
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
