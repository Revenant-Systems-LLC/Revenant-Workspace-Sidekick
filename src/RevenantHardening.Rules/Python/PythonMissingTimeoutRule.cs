using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-009: Missing timeout in HTTP requests.</summary>
public sealed partial class PythonMissingTimeoutRule : IRule
{
    // Match only the call head; the argument list is extracted with balanced-paren
    // scanning below, because a regex cannot reliably match nested parens
    // (e.g. min(a, b)) or argument lists that span multiple lines.
    [GeneratedRegex(@"requests\.(get|post|put|delete|patch|head|options|request)\s*\(", RegexOptions.Compiled)]
    private static partial Regex RequestsHeadRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-009",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in RequestsHeadRegex().Matches(context.Content))
        {
            // The match ends at the opening '(' of the call.
            var openParen = match.Index + match.Length - 1;
            var args = ExtractBalancedArgs(context.Content, openParen);

            if (!args.Contains("timeout="))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RSH-PY-009",
                    Title: $"Missing timeout in requests.{match.Groups[1].Value}()",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "HTTP requests without explicit timeouts can hang indefinitely if the server is unresponsive, leading to resource exhaustion.",
                    Fix: "Always specify a timeout (e.g., timeout=10) when making HTTP requests."
                );
            }
        }
    }

    /// <summary>
    /// Return the full argument text between the call's opening paren and its matching
    /// close paren, tracking nesting depth and skipping string literals. This prevents
    /// nested calls (e.g. min(a, b)), quoted parens, and multi-line argument lists from
    /// terminating the scan early — the cause of false "missing timeout" positives.
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
