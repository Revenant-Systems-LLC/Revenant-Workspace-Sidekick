using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Kotlin;

public sealed partial class KotlinForceUnwrapRule : IRule
{
    [GeneratedRegex(@"\w+!!(?![a-zA-Z_=])", RegexOptions.Compiled)]
    private static partial Regex ForceUnwrapRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-KT-003",
        Title: "Non-null assertion operator (!!)",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".kt", ".kts"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in ForceUnwrapRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "The !! operator throws NullPointerException if the value is null, crashing the app.",
                Fix: "Use safe calls (?.) or Elvis operator (?:)."
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
