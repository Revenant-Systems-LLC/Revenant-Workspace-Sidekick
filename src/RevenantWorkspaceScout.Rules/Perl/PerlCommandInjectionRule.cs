using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Perl;

public sealed partial class PerlCommandInjectionRule : IRule
{
    [GeneratedRegex(@"(?:\b(?:system|exec)\s*\(|`[^`]+`)", RegexOptions.Compiled)]
    private static partial Regex CommandInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PL-001",
        Title: "Command injection risk in Perl",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".pl", ".pm"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in CommandInjectionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Using backticks, system(), or exec() can allow command injection if user input is included.",
                Fix: "Use the multi-argument form of system() or exec() to bypass the shell."
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
