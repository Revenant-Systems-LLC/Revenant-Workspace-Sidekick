using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Rust;

public sealed partial class RustUnsafeBlockRule : IRule
{
    [GeneratedRegex(@"\bunsafe\s*\{", RegexOptions.Compiled)]
    private static partial Regex UnsafeRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-RS-001",
        Title: "Unsafe block in Rust",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".rs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in UnsafeRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Unsafe blocks bypass Rust's memory safety guarantees.",
                Fix: "Ensure the unsafe code is thoroughly audited and encapsulated."
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
