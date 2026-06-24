using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.TypeScript;

/// <summary>RWS-TS-004: TS/JS Prototype Pollution.</summary>
public sealed partial class TsPrototypePollutionRule : IRule
{
    [GeneratedRegex(@"\b__proto__\b")]
    private static partial Regex ProtoRegex();

    [GeneratedRegex(@"\bconstructor\.prototype\b")]
    private static partial Regex ConstructorProtoRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-TS-004",
        Title: "Prototype Pollution risk",
        DefaultSeverity: Severity.High,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // __proto__
        foreach (Match match in ProtoRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-TS-004",
                Title: "Dangerous '__proto__' reference detected",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Accessing or modifying '__proto__' directly allows attackers to execute Prototype Pollution attacks. If an attacker controls the keys parsed into an object, they can inject properties into the global Object prototype, leading to application crashes or remote code execution.",
                Fix: "Avoid direct __proto__ access. Use Object.create(null) for safe key-value dictionary lookups, or enforce schema validation (e.g. using Zod or Joi) to strip prototype keys."
            );
        }

        // constructor.prototype
        foreach (Match match in ConstructorProtoRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-TS-004",
                Title: "Dangerous 'constructor.prototype' reference",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Directly accessing 'constructor.prototype' is a common alternative pathway for Prototype Pollution vulnerabilities.",
                Fix: "Ensure all user inputs are sanitized and prototype-polluting keys (like 'constructor', 'prototype', '__proto__') are explicitly blocked during merge or clone operations."
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
