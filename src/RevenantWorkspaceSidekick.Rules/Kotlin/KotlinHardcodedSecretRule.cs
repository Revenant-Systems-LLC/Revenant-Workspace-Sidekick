using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Kotlin;

public sealed partial class KotlinHardcodedSecretRule : IRule
{
    [GeneratedRegex(@"(?i)val\s+(password|secret|apikey|token)\s*=\s*[""'][^""']+[""']", RegexOptions.Compiled)]
    private static partial Regex HardcodedSecretRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-KT-001",
        Title: "Hardcoded secret or credential",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".kt", ".kts"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in HardcodedSecretRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoded secrets can be easily extracted from source code/APK.",
                Fix: "Use BuildConfig, environment variables, or encrypted SharedPreferences."
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
