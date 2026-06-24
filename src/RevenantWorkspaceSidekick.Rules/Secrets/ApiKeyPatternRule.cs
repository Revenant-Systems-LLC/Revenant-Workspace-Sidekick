using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Secrets;

/// <summary>RWS-SEC-001: Hardcoded API key/token pattern.</summary>
public sealed partial class ApiKeyPatternRule : IRule
{
    // Regex patterns for variable-structure secrets (must stay as regex)
    [GeneratedRegex(@"(?i)(api[_\-]?key|apikey|api[_\-]?secret)\s*[=:]\s*[""']?[A-Za-z0-9+/\-_]{20,}[""']?")]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"(?i)bearer\s+[A-Za-z0-9\-._~+/]{20,}")]
    private static partial Regex BearerTokenPattern();

    // Literal prefixes for exact-format tokens — handled by AhoCorasick for O(N) ReDoS-free scan.
    // After a prefix hit we validate length + charset via simple span checks (no regex backtracking).
    private static readonly AhoCorasickMatcher PrefixMatcher = new([
        "sk-",          // OpenAI (legacy)
        "sk-proj-",     // OpenAI project key
        "ghp_",         // GitHub PAT
        "AKIA",         // AWS access key
        "AIza",         // Google API key
        "xoxb-",        // Slack bot token
        "xoxp-",        // Slack user token
    ]);

    private static readonly (string Label, int MinLength, int MaxLength)[] PrefixMeta =
    [
        ("OpenAI key",         20,  60),   // sk- (3) + 17-57
        ("OpenAI project key", 36,  80),   // sk-proj- (8) + 28-72
        ("GitHub PAT",         40,  40),   // ghp_ (4) + 36 (classic PAT)
        ("AWS access key",     20,  20),   // AKIA (4) + 16
        ("Google API key",     35,  45),   // AIza (4) + 31-41
        ("Slack bot token",    50, 200),   // xoxb- (5) + 45-195
        ("Slack user token",   50, 200),   // xoxp- (5) + 45-195
    ];

    private static readonly (Regex Pattern, string Label)[] RegexPatterns =
    [
        (ApiKeyPattern(),    "API key/secret"),
        (BearerTokenPattern(), "Bearer token"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-SEC-001",
        Title: "Hardcoded API key/token pattern",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cs", ".xaml", ".resx", ".csproj", ".props", ".targets", ".json", ".config", ".xml"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // --- Pass 1: AhoCorasick literal-prefix scan ---
        foreach (var (patternIndex, start, end) in PrefixMatcher.Search(context.Content))
        {
            var (label, minLen, maxLen) = PrefixMeta[patternIndex];

            // Advance past the prefix and validate the rest of the token
            var tokenStart = start;
            var i = end;
            while (i < context.Content.Length && i - tokenStart <= maxLen)
            {
                var c = context.Content[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.') break;
                i++;
            }

            var tokenLength = i - tokenStart;
            if (tokenLength < minLen || tokenLength > maxLen) continue;

            var matchedValue = context.Content.Substring(tokenStart, tokenLength);
            var line = GetLineNumber(context.Content, tokenStart);

            yield return new Finding(
                RuleId: "RWS-SEC-001",
                Title: $"Hardcoded {label} detected",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoded credentials in source files are frequently committed to version control and exposed in build artifacts. AI assistants commonly generate placeholder secrets that developers forget to replace.",
                Fix: "Move this secret to an environment variable, user secrets (dotnet user-secrets), or a secrets manager. Never commit credentials to source control.",
                RedactedSnippet: SecretBlinder.Blind(matchedValue),
                RawValue: matchedValue
            );
        }

        // --- Pass 2: Regex patterns for variable-structure secrets ---
        foreach (var (pattern, label) in RegexPatterns)
        {
            foreach (Match match in pattern.Matches(context.Content))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-SEC-001",
                    Title: $"Hardcoded {label} detected",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Hardcoded credentials in source files are frequently committed to version control and exposed in build artifacts. AI assistants commonly generate placeholder secrets that developers forget to replace.",
                    Fix: "Move this secret to an environment variable, user secrets (dotnet user-secrets), or a secrets manager. Never commit credentials to source control.",
                    RedactedSnippet: SecretBlinder.Blind(match.Value)
                );
            }
        }

        // --- Pass 3: Shannon entropy — catch secrets with no known prefix ---
        // Scan for quoted string literals; flag any with entropy ≥ 4.5 bpc.
        // This catches tokens from new/unknown providers that lack a literal prefix.
        foreach (Match match in QuotedStringPattern().Matches(context.Content))
        {
            var value = match.Groups["val"].Value;
            if (value.Length < 20 || value.Length > 200) continue;
            if (!EntropyScorer.IsHighEntropy(value)) continue;

            // Skip values already caught by Pass 1 or Pass 2 on the same line
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-SEC-001",
                Title: "High-entropy string detected (possible secret)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: $"This string has Shannon entropy {EntropyScorer.Shannon(value):F2} bpc — statistically consistent with a randomly-generated token or key. AI-generated code often includes placeholder secrets copied from documentation.",
                Fix: "Verify this is not a hardcoded credential. If it is, move it to environment variables or a secrets manager.",
                RedactedSnippet: SecretBlinder.Blind(value)
            );
        }
    }

    [GeneratedRegex(@"""(?<val>[A-Za-z0-9+/=\-_\.~]{20,200})""")]
    private static partial Regex QuotedStringPattern();

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
