using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Java;

/// <summary>RWS-JV-009: Unbounded loops (while(true) without break/return).</summary>
public sealed partial class JavaUnboundedLoopRule : IRule
{
    [GeneratedRegex(@"while\s*\(\s*true\s*\)\s*\{", RegexOptions.Compiled)]
    private static partial Regex WhileTrueRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-009",
        Title: "Potentially unbounded loop",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var content = context.Content;
        foreach (Match match in WhileTrueRegex().Matches(content))
        {
            // Simple brace matching to extract the loop body
            int braceCount = 1;
            int endIndex = -1;
            
            for (int i = match.Index + match.Length; i < content.Length; i++)
            {
                if (content[i] == '{') braceCount++;
                else if (content[i] == '}') braceCount--;
                
                if (braceCount == 0)
                {
                    endIndex = i;
                    break;
                }
            }
            
            if (endIndex != -1)
            {
                var body = content.Substring(match.Index, endIndex - match.Index);
                if (!Regex.IsMatch(body, @"\b(break|return|throw)\b"))
                {
                    var line = GetLineNumber(context.Content, match.Index);
                    yield return new Finding(
                        RuleId: "RWS-JV-009",
                        Title: "Unbounded 'while(true)' loop",
                        Severity: Severity.Medium,
                        File: context.RelativePath,
                        Line: line,
                        Why: "Loops without an exit condition (break, return, or throw) can run infinitely, causing resource exhaustion.",
                        Fix: "Ensure the loop has a clear exit condition or bounded iterations."
                    );
                }
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
