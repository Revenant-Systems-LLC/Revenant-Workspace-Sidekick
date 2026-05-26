using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-003: TS/JS Insecure Cryptography.</summary>
public sealed partial class TsInsecureCryptoRule : IRule
{
    [GeneratedRegex(@"\bcreateHash\s*\(\s*['""`](md5|sha1)['""`]\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex InsecureHashRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-003",
        Title: "Weak cryptographic hashing",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in InsecureHashRegex().Matches(context.Content))
        {
            var hash = match.Groups[1].Value.ToUpper();
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-003",
                Title: $"Use of weak cryptographic hash {hash}",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: $"The '{hash}' hashing algorithm is cryptographically weak, broken, and susceptible to collision attacks.",
                Fix: "Use cryptographically secure hashes like SHA-256 or SHA-512 (e.g. crypto.createHash('sha256'))."
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
