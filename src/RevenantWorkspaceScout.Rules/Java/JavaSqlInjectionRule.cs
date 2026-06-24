using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Java;

/// <summary>RWS-JV-007: SQL Injection via string concatenation.</summary>
public sealed partial class JavaSqlInjectionRule : IRule
{
    [GeneratedRegex(@"(?i)""\s*(select|insert|update|delete)\b.*?(from|into|table).*?""\s*\+\s*\w+", RegexOptions.Compiled)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-007",
        Title: "SQL Injection vulnerability",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SqlInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-007",
                Title: "SQL Injection via dynamic query construction",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Constructing SQL queries using string concatenation allows attackers to inject malicious SQL commands.",
                Fix: "Use PreparedStatement or an ORM like Hibernate/JPA to safely execute SQL statements."
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
