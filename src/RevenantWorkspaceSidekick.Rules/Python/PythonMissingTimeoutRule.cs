using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-009: Missing timeout in HTTP requests.</summary>
public sealed partial class PythonMissingTimeoutRule : IRule
{
    [GeneratedRegex(@"requests\.(get|post|put|delete|patch|head|options|request)\s*\((.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex RequestsRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-009",
        Title: "Missing timeout in HTTP request",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in RequestsRegex().Matches(context.Content))
        {
            var args = match.Groups[2].Value;

            if (!args.Contains("timeout="))
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

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
