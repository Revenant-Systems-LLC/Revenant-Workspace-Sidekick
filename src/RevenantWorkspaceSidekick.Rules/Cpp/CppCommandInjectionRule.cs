using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Cpp;

/// <summary>RWS-CPP-004: Dangerous use of system() or popen().</summary>
public sealed partial class CppCommandInjectionRule : IRule
{
    [GeneratedRegex(@"\b(system|popen|exec[lv]p?e?)\s*\(", RegexOptions.Compiled)]
    private static partial Regex SystemCallRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-CPP-004",
        Title: "Command injection via system()/popen()",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cpp", ".c", ".cc", ".cxx", ".h", ".hpp"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in SystemCallRegex().Matches(context.Content))
        {
            var funcName = match.Groups[1].Value;
            var line = GetLineNumber(context.Content, match.Index);

            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith("//") || lineText.TrimStart().StartsWith("*"))
                continue;

            yield return new Finding(
                RuleId: "RWS-CPP-004",
                Title: $"Dangerous call to '{funcName}()' — command injection risk",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: $"'{funcName}()' passes a string to the shell for execution. If any part of the string is user-controlled, an attacker can inject arbitrary commands.",
                Fix: "Avoid shell execution. Use fork/exec with explicit argument arrays, or a library like Boost.Process with argument lists."
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

    private static string GetLineText(string content, int charIndex)
    {
        var start = content.LastIndexOf('\n', Math.Max(0, charIndex - 1)) + 1;
        var end = content.IndexOf('\n', charIndex);
        if (end < 0) end = content.Length;
        return content[start..end];
    }
}
