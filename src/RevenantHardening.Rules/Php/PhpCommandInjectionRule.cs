using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Php;

public sealed partial class PhpCommandInjectionRule : IRule
{
    [GeneratedRegex(@"(?i)\b(shell_exec|exec|system|passthru)\s*\(|`[^`]+`", RegexOptions.Compiled)]
    private static partial Regex CommandInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PHP-001",
        Title: "Command injection risk in PHP",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".php"]
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
                Why: "Using shell execution functions can allow command injection if user input is included.",
                Fix: "Avoid shell commands, use built-in PHP functions, or use escapeshellarg()."
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
