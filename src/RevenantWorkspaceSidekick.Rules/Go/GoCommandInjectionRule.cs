using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Go;

public sealed partial class GoCommandInjectionRule : IRule
{
    [GeneratedRegex(@"exec\.Command\s*\([^,]+,\s*[a-zA-Z_]\w*\s*\)", RegexOptions.Compiled)]
    private static partial Regex CommandInjectionRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-GO-001",
        Title: "Command injection risk in os/exec",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".go"]
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
                Why: "Executing commands with variable arguments can lead to command injection if not properly sanitized.",
                Fix: "Ensure all arguments passed to exec.Command are strictly validated."
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
