using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Rust;

/// <summary>RWS-RS-003: Detect Command::new with format! or string concatenation (command injection risk).</summary>
public sealed partial class RustCommandInjectionRule : IRule
{
    [GeneratedRegex(@"Command::new\s*\(\s*(?:&?\s*format!\s*\(|[^"")\s]+\s*\+)", RegexOptions.Compiled)]
    private static partial Regex CommandFormatRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-RS-003",
        Title: "Command injection via Command::new with dynamic input",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".rs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in CommandFormatRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//"))
                continue;

            yield return new Finding(
                RuleId: "RWS-RS-003",
                Title: "Command injection — Command::new with dynamic string",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Passing a dynamically constructed string (format!, concatenation) to Command::new allows an attacker to control which binary is executed if any part of the string is user-derived.",
                Fix: "Use a static command name in Command::new and pass dynamic values via .arg() or .args() to avoid shell interpretation."
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

    private static string GetLineText(string content, int charIndex)
    {
        var start = content.LastIndexOf('\n', Math.Max(0, charIndex - 1)) + 1;
        var end = content.IndexOf('\n', charIndex);
        if (end < 0) end = content.Length;
        return content[start..end];
    }
}
