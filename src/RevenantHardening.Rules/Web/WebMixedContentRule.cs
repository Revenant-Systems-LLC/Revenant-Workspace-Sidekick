using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Web;

public sealed partial class WebMixedContentRule : IRule
{
    [GeneratedRegex(@"(src|href)=[""']http://", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MixedContentRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-WEB-001",
        Title: "Mixed content (HTTP URL on HTTPS site)",
        DefaultSeverity: Severity.High,
        FileExtensions: [".html", ".htm"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in MixedContentRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Loading resources over HTTP can lead to mixed content blocking by browsers and Man-in-the-Middle attacks.",
                Fix: "Use HTTPS for all external resources."
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
