using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Php;

public sealed partial class PhpSqlInjectionRule : IRule
{
    [GeneratedRegex(@"(?i)""SELECT.*?\$_(GET|POST|REQUEST)", RegexOptions.Compiled)]
    private static partial Regex SqlInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PHP-002",
        Title: "SQL Injection risk via string concatenation",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".php"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SqlInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Directly concatenating superglobals into SQL queries allows SQL injection.",
                Fix: "Use PDO prepared statements."
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
