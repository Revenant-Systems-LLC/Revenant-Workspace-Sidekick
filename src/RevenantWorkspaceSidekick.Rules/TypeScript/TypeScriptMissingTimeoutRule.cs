using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.TypeScript;

/// <summary>RWS-TS-010: Missing timeout in fetch/axios requests.</summary>
public sealed partial class TypeScriptMissingTimeoutRule : IRule
{
    // Match only the call head; arguments are extracted with balanced-paren scanning
    // below so nested calls, quoted parens, and multi-line argument lists don't
    // truncate the scan early and produce false "missing timeout" positives.
    [GeneratedRegex(@"(?i)\b(axios\.(get|post|put|delete|patch)|fetch)\s*\(", RegexOptions.Compiled)]
    private static partial Regex HttpHeadRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-TS-010",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in HttpHeadRegex().Matches(context.Content))
        {
            // The regex ends with \( so the last char of the match is the opening paren.
            var openParen = match.Index + match.Length - 1;
            var args = ExtractBalancedArgs(context.Content, openParen);
            var funcName = match.Groups[1].Value;

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

    // Walk the content from openParenIndex, tracking paren depth, and return the argument
    // text between the opening '(' and its matching ')'. Parens inside string literals
    // (including template literals) are skipped.
    private static string ExtractBalancedArgs(string content, int openParenIndex)
    {
        var depth = 0;
        var quote = '\0';
        var start = openParenIndex + 1;
        for (var i = openParenIndex; i < content.Length; i++)
        {
            var c = content[i];

            if (quote != '\0')
            {
                if (c == quote && content[i - 1] != '\\')
                    quote = '\0';
                continue;
            }

            switch (c)
            {
                case '\'':
                case '"':
                case '`':
                    quote = c;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    if (depth == 0)
                        return content.Substring(start, i - start);
                    break;
            }
        }

        return content[start..];
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
