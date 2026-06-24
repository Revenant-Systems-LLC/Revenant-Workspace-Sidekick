using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.TypeScript;

/// <summary>RWS-TS-010: Missing timeout in fetch/axios requests.</summary>
public sealed partial class TypeScriptMissingTimeoutRule : IRule
{
    // Check for axios/fetch calls without explicit timeout settings
    [GeneratedRegex(@"(?i)\b(axios\.(get|post|put|delete|patch)|fetch)\s*\((.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex HttpRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-TS-010",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in HttpRegex().Matches(context.Content))
        {
            var funcName = match.Groups[1].Value;
            var args = match.Groups[3].Value;

            // Fetch does not have a timeout property natively, it uses AbortController
            // Axios uses `timeout: X` in the config object
            if (!args.Contains("timeout") && !args.Contains("AbortSignal") && !args.Contains("signal"))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-TS-010",
                    Title: $"Missing timeout in {funcName}()",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "HTTP requests without explicit timeouts or AbortSignals can hang indefinitely if the server is unresponsive.",
                    Fix: "For fetch, use AbortController. For axios, specify a timeout in the request config."
                );
            }
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
