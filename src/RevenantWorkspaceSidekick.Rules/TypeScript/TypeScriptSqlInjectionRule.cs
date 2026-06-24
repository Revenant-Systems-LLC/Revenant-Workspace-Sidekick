using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.TypeScript;

/// <summary>RWS-TS-007: SQL Injection via string concatenation.</summary>
public sealed partial class TypeScriptSqlInjectionRule : IRule
{
    // Match template literals or concatenation
    [GeneratedRegex(@"(?i)`\s*(select|insert|update|delete)\b.*?(from|into|table).*?\$\{.+?\}.*?`|[""']\s*(select|insert|update|delete)\b.*?(from|into|table).*?[""']\s*\+\s*\w+", RegexOptions.Compiled)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-TS-007",
        Title: "SQL Injection vulnerability",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SqlInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-TS-007",
                Title: "SQL Injection via dynamic query construction",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Constructing SQL queries using template literals or string concatenation allows attackers to inject malicious SQL commands.",
                Fix: "Use parameterized queries, prepared statements, or an ORM like Prisma/TypeORM."
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
