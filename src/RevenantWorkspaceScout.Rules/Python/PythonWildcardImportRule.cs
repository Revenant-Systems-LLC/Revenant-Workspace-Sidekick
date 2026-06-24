using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-014: Wildcard imports (from module import *).</summary>
public sealed partial class PythonWildcardImportRule : IRule
{
    [GeneratedRegex(@"^\s*from\s+\S+\s+import\s+\*", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex WildcardImportRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-014",
        Title: "Wildcard import (import *)",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in WildcardImportRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-PY-014",
                Title: "Wildcard import pollutes the namespace",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "'from module import *' imports every name from the module into the current namespace, making it unclear where names come from and risking name collisions.",
                Fix: "Import only what you need: 'from math import sqrt, pi' or use 'import math' and qualify calls as 'math.sqrt()'."
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
