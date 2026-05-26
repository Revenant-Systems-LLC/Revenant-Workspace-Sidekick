using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-005: Insecure Web Framework Configuration.</summary>
public sealed partial class InsecureWebConfigRule : IRule
{
    [GeneratedRegex(@"\bdebug\s*=\s*True\b")]
    private static partial Regex DebugTrueRegex();

    [GeneratedRegex(@"\bSECRET_KEY\s*=\s*['""]([^'""\r\n]{8,})['""]")]
    private static partial Regex SecretKeyRegex();

    [GeneratedRegex(@"\b(CORS_ORIGIN_ALLOW_ALL\s*=\s*True|origins\s*=\s*['""]\*['""]|allowed_origins\s*=\s*\[[^\]]*['""]\*['""][^\]]*\])", RegexOptions.IgnoreCase)]
    private static partial Regex WildcardCorsRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-005",
        Title: "Insecure web application configuration",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // 1. Debug mode enabled
        foreach (Match match in DebugTrueRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-005",
                Title: "Framework debug mode enabled",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Enabling debug mode in production (e.g. Flask/Django DEBUG = True) exposes verbose error stacks, system details, and interactive debug consoles that allow remote code execution.",
                Fix: "Ensure debug mode is disabled in production. Use configuration management or environment variables (e.g., debug=os.environ.get('DEBUG') == 'True')."
            );
        }

        // 2. Hardcoded SECRET_KEY
        foreach (Match match in SecretKeyRegex().Matches(context.Content))
        {
            var keyVal = match.Groups[1].Value;
            // Ignore obvious environment loading or placeholders like 'your-key' if too short or look like environment loads
            if (keyVal.Contains("os.environ") || keyVal.Contains("getenv"))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-005",
                Title: "Hardcoded SECRET_KEY in web configuration",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "A hardcoded SECRET_KEY in source files is committed to version control. If compromised, it allows attackers to forge session cookies, reset passwords, or bypass cryptographic protections.",
                Fix: "Load the SECRET_KEY from an environment variable: os.environ.get('SECRET_KEY') or a secret file."
            );
        }

        // 3. CORS Wildcard / Allow All
        foreach (Match match in WildcardCorsRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-005",
                Title: "Wildcard CORS origin allowed",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Allowing all origins ('*') for CORS headers can expose sensitive backend resources to unauthorized requests originating from third-party websites.",
                Fix: "Explicitly list authorized origins instead of using the wildcard '*' origin."
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
