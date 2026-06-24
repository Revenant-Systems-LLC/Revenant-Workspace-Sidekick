using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Java;

/// <summary>RWS-JV-003: Java Weak Cryptography.</summary>
public sealed partial class JavaWeakCryptoRule : IRule
{
    [GeneratedRegex(@"\bMessageDigest\.getInstance\s*\(\s*""(MD5|SHA-1)""\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex WeakHashRegex();

    [GeneratedRegex(@"\bCipher\.getInstance\s*\(\s*""(DES|Blowfish|AES/ECB/[^""]*)""\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex WeakCipherRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-003",
        Title: "Weak cryptographic configuration",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // MD5/SHA-1
        foreach (Match match in WeakHashRegex().Matches(context.Content))
        {
            var hash = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-003",
                Title: $"Weak cryptographic hash algorithm {hash}",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: $"The '{hash}' algorithm contains severe collision vulnerabilities and is cryptographically broken.",
                Fix: "Use cryptographically secure hashing functions like SHA-256 or SHA-512 (e.g. MessageDigest.getInstance(\"SHA-256\"))."
            );
        }

        // DES/Blowfish/ECB
        foreach (Match match in WeakCipherRegex().Matches(context.Content))
        {
            var cipher = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-003",
                Title: $"Insecure Cipher configuration: {cipher}",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: $"The cipher mode '{cipher}' is weak. DES and Blowfish are deprecated, and ECB mode (Electronic Codebook) is insecure because it encrypts identical plaintext blocks into identical ciphertext blocks, revealing patterns.",
                Fix: "Use AES in GCM mode (e.g., Cipher.getInstance(\"AES/GCM/NoPadding\")) for authenticated encryption."
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
