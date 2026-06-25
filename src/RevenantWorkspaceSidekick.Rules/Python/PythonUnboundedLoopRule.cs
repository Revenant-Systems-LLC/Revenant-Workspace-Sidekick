using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-010: Unbounded loops (while True without break/return).</summary>
public sealed partial class PythonUnboundedLoopRule : IRule
{
    // Match 'while True:' and we will check the lines after it
    [GeneratedRegex(@"while\s+True\s*:", RegexOptions.Compiled)]
    private static partial Regex WhileTrueRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-010",
        Title: "Potentially unbounded loop",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var lines = context.Content.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = WhileTrueRegex().Match(line);
            
            if (match.Success)
            {
                // Find indentation level
                var indentMatch = Regex.Match(line, @"^\s*");
                var loopIndent = indentMatch.Length;
                
                bool hasExit = false;
                int j = i + 1;
                
                // Scan the loop body
                while (j < lines.Length)
                {
                    var bodyLine = lines[j];
                    var bodyIndentMatch = Regex.Match(bodyLine, @"^\s*");
                    
                    // Ignore empty lines
                    if (string.IsNullOrWhiteSpace(bodyLine))
                    {
                        j++;
                        continue;
                    }
                    
                    // If we dedent to the loop's level or lower, the loop body ended
                    if (bodyIndentMatch.Length <= loopIndent)
                    {
                        break;
                    }
                    
                    // Check for break, return, or raise
                    if (Regex.IsMatch(bodyLine, @"\b(break|return|raise)\b"))
                    {
                        hasExit = true;
                        break;
                    }
                    
                    j++;
                }
                
                if (!hasExit)
                {
                    yield return new Finding(
                        RuleId: "RWS-PY-010",
                        Title: "Unbounded 'while True' loop",
                        Severity: Severity.Medium,
                        File: context.RelativePath,
                        Line: i + 1,
                        Why: "Loops without an exit condition (break, return, or raise) can run infinitely, causing resource exhaustion and deadlocks.",
                        Fix: "Ensure the loop has a clear exit condition or bounded iterations."
                    );
                }
            }
        }
    }
}
