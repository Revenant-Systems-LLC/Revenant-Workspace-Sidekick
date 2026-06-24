using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.TypeScript;

/// <summary>RWS-TS-001: TS/JS dynamic code evaluation.</summary>
public sealed partial class TsDangerousEvalRule : IRule
{
    [GeneratedRegex(@"\beval\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex EvalRegex();

    [GeneratedRegex(@"\bnew\s+Function\s*\(")]
    private static partial Regex NewFunctionRegex();

    [GeneratedRegex(@"\b(setTimeout|setInterval)\s*\(\s*['""`]([^'""`]*)['""`]")]
    private static partial Regex InsecureTimerRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-TS-001",
        Title: "TS/JS dynamic code evaluation",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // eval(...)
        foreach (Match match in EvalRegex().Matches(context.Content))
        {
            var arg = match.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(arg))
                continue;

            // Check if it is a simple static literal
            bool isLiteral = (arg.StartsWith("'") && arg.EndsWith("'")) ||
                             (arg.StartsWith("\"") && arg.EndsWith("\"")) ||
                             (arg.StartsWith("`") && arg.EndsWith("`"));

            if (arg.Contains("${") || !isLiteral)
                isLiteral = false;

            if (!isLiteral)
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-TS-001",
                    Title: "Dangerous use of eval()",
                    Severity: Severity.Critical,
                    File: context.RelativePath,
                    Line: line,
                    Why: "The 'eval()' function executes arbitrary JavaScript code in the caller's privileges. When called with dynamic or user-influenced values, it creates remote code execution (RCE) vulnerabilities.",
                    Fix: "Avoid eval entirely. Parse structured data using JSON.parse() or use safe static function lookups."
                );
            }
        }

        // new Function()
        foreach (Match match in NewFunctionRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-TS-001",
                Title: "Dangerous dynamic function creation (Function constructor)",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Creating functions dynamically using 'new Function()' compiles strings into executable code, similar to eval(), making it highly prone to injection attacks.",
                Fix: "Avoid new Function(). Write standard declared functions or closures instead."
            );
        }

        // setTimeout/setInterval with string
        foreach (Match match in InsecureTimerRegex().Matches(context.Content))
        {
            var timerType = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-TS-001",
                Title: $"Dangerous string argument in {timerType}()",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: $"Passing a string instead of a function callback to '{timerType}()' compiles and executes it dynamically, creating an eval-like injection risk.",
                Fix: "Pass a callback function instead: setTimeout(() => { ... }, 1000)."
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
