using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Java;

/// <summary>RWS-JV-004: Java Path Traversal.</summary>
public sealed partial class JavaPathTraversalRule : IRule
{
    [GeneratedRegex(@"\bnew\s+File\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex FileInstantiationRegex();

    [GeneratedRegex(@"\bPaths\.get\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex PathsGetRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-004",
        Title: "Java path traversal vulnerability",
        DefaultSeverity: Severity.High,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // new File(...)
        foreach (Match match in FileInstantiationRegex().Matches(context.Content))
        {
            var arg = match.Groups[1].Value.Trim();
            if (IsDynamic(arg))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-JV-004",
                    Title: "Potential path traversal in file creation",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Constructing file paths using string concatenation or variables derived from user inputs can allow Directory Traversal attacks (e.g., passing '../../etc/passwd' to access protected files).",
                    Fix: "Validate and sanitize any file path parameters before executing operations. Use path normalization (Path.normalize()) and verify that the target path remains within the intended root directory."
                );
            }
        }

        // Paths.get(...)
        foreach (Match match in PathsGetRegex().Matches(context.Content))
        {
            var arg = match.Groups[1].Value.Trim();
            if (IsDynamic(arg))
            {
                var line = GetLineNumber(context.Content, match.Index);
                yield return new Finding(
                    RuleId: "RWS-JV-004",
                    Title: "Potential path traversal via Paths.get()",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: line,
                    Why: "Using Paths.get() with dynamic values or string concatenations allows directories outside the intended workspace to be accessed or written to.",
                    Fix: "Ensure all parameters are fully sanitized and resolve the final path against a secure base path (e.g., using securePath.startsWith(basePath))."
                );
            }
        }
    }

    private static bool IsDynamic(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        bool isLiteral = arg.StartsWith("\"") && arg.EndsWith("\"");
        if (arg.Contains("+") || !isLiteral)
            isLiteral = false;

        return !isLiteral;
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
