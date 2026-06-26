using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-009: Missing timeout in HTTP requests.</summary>
public sealed partial class PythonMissingTimeoutRule : IRule
{
    // Match the method name and opening paren only; args are extracted via balanced-paren walk
    // so multi-line calls with nested function calls (e.g. headers=foo()) are handled correctly.
    [GeneratedRegex(@"requests\.(get|post|put|delete|patch|head|options|request)\s*\(", RegexOptions.Compiled)]
    private static partial Regex RequestsCallRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-009",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in RequestsCallRegex().Matches(context.Content))
        {
            // The regex ends with \( so the last char of the match is the opening paren.
            var openParen = match.Index + match.Length - 1;
            var callBody = ExtractBalancedParens(context.Content, openParen);

            if (!callBody.Contains("timeout="))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-PY-009",
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

    // Walk the content from openParenIndex, tracking paren depth, and return everything
    // from the opening '(' to the matching closing ')' inclusive.
    private static string ExtractBalancedParens(string content, int openParenIndex)
    {
        var depth = 0;
        for (var i = openParenIndex; i < content.Length; i++)
        {
            if (content[i] == '(') depth++;
            else if (content[i] == ')') { depth--; if (depth == 0) return content[openParenIndex..(i + 1)]; }
        }
        return content[openParenIndex..];
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
