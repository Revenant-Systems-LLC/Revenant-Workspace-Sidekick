using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Dart;

/// <summary>RWS-DT-001: Dangerous use of dart:mirrors or eval-like patterns.</summary>
public sealed partial class DartDynamicCodeRule : IRule
{
    [GeneratedRegex(@"\bimport\s+[""']dart:mirrors[""']", RegexOptions.Compiled)]
    private static partial Regex MirrorsRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-DT-001",
        Title: "Use of dart:mirrors (reflection)",
        DefaultSeverity: Severity.High,
        FileExtensions: [".dart"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in MirrorsRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-DT-001",
                Title: "Import of dart:mirrors (reflection)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "dart:mirrors enables runtime reflection which breaks tree-shaking, increases binary size, and is unavailable in Flutter/AOT contexts.",
                Fix: "Use code generation (build_runner/json_serializable) or explicit factories instead of reflection."
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
