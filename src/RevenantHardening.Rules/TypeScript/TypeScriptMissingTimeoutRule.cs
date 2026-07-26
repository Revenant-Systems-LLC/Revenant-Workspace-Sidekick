using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-010: Missing timeout in fetch/axios requests.</summary>
public sealed partial class TypeScriptMissingTimeoutRule : IRule
{
    // Match only the call head; arguments are extracted with balanced-paren
    // scanning below so nested parens and multi-line calls don't truncate early.
    [GeneratedRegex(@"(?i)\b(axios\.(get|post|put|delete|patch)|fetch)\s*\(", RegexOptions.Compiled)]
    private static partial Regex HttpHeadRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-010",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".ts", ".js", ".tsx", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in HttpHeadRegex().Matches(context.Content))
        {
            var openParen = match.Index + match.Length - 1;
            var args = ExtractBalancedArgs(context.Content, openParen);
            var funcName = match.Groups[1].Value;

            if (!args.Contains("timeout") && !args.Contains("AbortSignal") && !args.Contains("signal"))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RSH-TS-010",
                    Title: $"Missing timeout in {funcName}()",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "HTTP requests without explicit timeouts or AbortSignals can hang indefinitely if the server is unresponsive.",
                    Fix: "For fetch, use AbortController with a timeout. For axios, specify timeout in the request config."
                );
            }
        }
    }

    /// <summary>
    /// Return the full argument text between the call's opening paren and its matching
    /// close paren, tracking nesting depth and skipping string literals.
    /// </summary>
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
