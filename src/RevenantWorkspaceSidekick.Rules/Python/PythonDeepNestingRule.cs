using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Python;

/// <summary>RWS-PY-016: Deep nesting (>3 levels).</summary>
public sealed partial class PythonDeepNestingRule : IRule
{
    [GeneratedRegex(@"^(\s*)(if |elif |else:|for |while |try:|except |with )", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex ControlFlowRegex();

    // Python standard indentation is 4 spaces
    private const int IndentSize = 4;
    private const int MaxDepth = 3;

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-016",
        Title: "Deeply nested control flow",
        DefaultSeverity: Severity.Info,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var reported = new HashSet<int>(); // Avoid duplicate reports per line

        foreach (Match match in ControlFlowRegex().Matches(context.Content))
        {
            var indent = match.Groups[1].Value;

            // Calculate nesting depth from indentation
            int spaces = 0;
            foreach (char c in indent)
            {
                if (c == ' ') spaces++;
                else if (c == '\t') spaces += IndentSize;
            }

            int depth = spaces / IndentSize;

            if (depth >= MaxDepth)
            {
                var line = GetLineNumber(context.Content, match.Index);
                if (reported.Add(line))
                {
                    yield return new Finding(
                        RuleId: "RWS-PY-016",
                        Title: $"Control flow nested {depth + 1} levels deep",
                        Severity: Severity.Info,
                        File: context.RelativePath,
                        Line: line,
                        Why: $"Code nested more than {MaxDepth} levels deep is hard to read and maintain. This is a sign the function is doing too much.",
                        Fix: "Extract deeply nested logic into helper functions or use early returns (guard clauses) to reduce nesting.",
                        Example: """
                            # Bad
                            if user:
                                if user.is_active:
                                    if user.has_subscription:
                                        if not user.expired:
                                            do_something()

                            # Good (Guard Clauses)
                            if not user or not user.is_active:
                                return
                            if not user.has_subscription or user.expired:
                                return
                            do_something()
                            """
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
