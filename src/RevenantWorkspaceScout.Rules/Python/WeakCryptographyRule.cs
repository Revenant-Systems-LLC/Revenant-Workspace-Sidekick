using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-004: Weak Cryptography/Hashing.</summary>
public sealed partial class WeakCryptographyRule : IRule
{
    [GeneratedRegex(@"\bhashlib\.(md5|sha1)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex WeakHashRegex();

    [GeneratedRegex(@"\bcrypt\.crypt\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex CryptRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-004",
        Title: "Weak cryptographic algorithm",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // MD5/SHA1
        foreach (Match match in WeakHashRegex().Matches(context.Content))
        {
            var hashType = match.Groups[1].Value.ToUpper();
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-004",
                Title: $"Use of weak hash algorithm {hashType}",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: $"The {hashType} algorithm is cryptographically broken and vulnerable to collision attacks. It should not be used for secure hashing, signature validation, or password storage.",
                Fix: "Use stronger hashing algorithms like SHA-256 or SHA-512 (e.g. hashlib.sha256()). For password hashing, use bcrypt, Argon2, or PBKDF2."
            );
        }

        // crypt.crypt
        foreach (Match match in CryptRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-004",
                Title: "Use of insecure crypt.crypt()",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "The legacy 'crypt' module uses outdated hashing methods (like DES) by default which are easily crackable. It is also deprecated in Python 3.11+.",
                Fix: "Use modern secure libraries like 'bcrypt' or 'hashlib.pbkdf2_hmac' for password hashing and validation."
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
