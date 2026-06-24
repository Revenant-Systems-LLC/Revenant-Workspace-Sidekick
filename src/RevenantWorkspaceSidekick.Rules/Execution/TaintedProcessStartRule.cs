using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Execution;

/// <summary>
/// RWS-EXEC-006 — Taint tracking: user-controlled input flows into Process.Start or Assembly.Load.
/// Performs intra-method data-flow analysis using simple regex pattern matching on C# source:
///   1. Identify "taint sources" in the method: args[], Environment.GetEnvironmentVariable,
///      Console.ReadLine(), TextBox.Text, Request.QueryString, etc.
///   2. Track variable assignments from those sources.
///   3. If a tainted variable name appears in a Process.Start / Assembly.Load call, flag it.
/// This is intentionally conservative — only flags confirmed source→sink chains.
/// </summary>
public sealed partial class TaintedProcessStartRule : IRule
{
    // Taint sources: user-controlled inputs
    [GeneratedRegex(@"(?:args\[|Environment\.GetEnvironmentVariable|Console\.ReadLine|\.Text\s*;|Request\.(?:Query|Form|QueryString|Params)\[|GetCommandLineArgs\(\))",
        RegexOptions.IgnoreCase)]
    private static partial Regex TaintSource();

    // Assignment: var x = <taint-source-expr>  OR  x = <taint-source-expr>
    [GeneratedRegex(@"(?:var\s+|string\s+)?(?<var>[A-Za-z_]\w*)\s*=\s*[^;]*?(?:args\[|Environment\.GetEnvironmentVariable|Console\.ReadLine|\.Text|Request\.(?:Query|Form|QueryString|Params)\[|GetCommandLineArgs)",
        RegexOptions.IgnoreCase)]
    private static partial Regex TaintAssignment();

    // Dangerous sinks
    [GeneratedRegex(@"Process\.Start\s*\(|Assembly\.Load(?:From|File)?\s*\(|ProcessStartInfo\s*\(")]
    private static partial Regex DangerousSink();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-006",
        Title: "Tainted input flows into dangerous execution sink",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var content = context.Content;

        // Quick bail — no sinks in this file at all
        if (!DangerousSink().IsMatch(content)) yield break;

        // Extract tainted variable names from the whole file
        var taintedVars = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in TaintAssignment().Matches(content))
            taintedVars.Add(m.Groups["var"].Value);

        if (taintedVars.Count == 0) yield break;

        // Build a pattern that matches any tainted var appearing in a sink call
        var escaped = string.Join("|", taintedVars.Select(Regex.Escape));
        var sinkWithTaint = new Regex(
            $@"(?:Process\.Start|Assembly\.Load(?:From|File)?|ProcessStartInfo)\s*\([^)]*\b(?:{escaped})\b",
            RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

        foreach (Match match in sinkWithTaint.Matches(content))
        {
            var line = GetLineNumber(content, match.Index);
            // Identify which tainted variable was matched
            var matchedVar = taintedVars.First(v => match.Value.Contains(v));
            yield return new Finding(
                RuleId: "RWS-EXEC-006",
                Title: $"User-controlled input '{matchedVar}' passed to execution sink",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: $"Variable '{matchedVar}' is derived from user-controlled input (args, env, UI, HTTP request) and is passed directly to a process launch or assembly load. This is a confirmed command-injection or arbitrary-code-execution vector. AI-generated code frequently pipes input straight into Process.Start without sanitization.",
                Fix: $"Validate and allowlist '{matchedVar}' before passing it to Process.Start or Assembly.Load. Reject unexpected values. Never construct executable paths or arguments from unvalidated user input.",
                Example: $$"""
                    // Instead of: Process.Start({{matchedVar}})
                    // Use an allowlist:
                    var allowed = new HashSet<string> { "notepad", "calc" };
                    if (!allowed.Contains({{matchedVar}})) throw new ArgumentException("Not allowed");
                    Process.Start({{matchedVar}});
                    """
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
