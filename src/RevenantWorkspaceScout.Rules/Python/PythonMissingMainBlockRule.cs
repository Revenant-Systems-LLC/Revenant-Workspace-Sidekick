using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Python;

/// <summary>RWS-PY-015: Missing if __name__ == '__main__' guard.</summary>
public sealed partial class PythonMissingMainBlockRule : IRule
{
    [GeneratedRegex(@"if\s+__name__\s*==\s*[""']__main__[""']", RegexOptions.Compiled)]
    private static partial Regex MainGuardRegex();

    // Match top-level function calls (no leading whitespace, not a def/class/import/comment/decorator)
    [GeneratedRegex(@"^(?!#|def |class |import |from |@|\s)(\w+\s*\()", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex TopLevelCallRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PY-015",
        Title: "Missing if __name__ == '__main__' guard",
        DefaultSeverity: Severity.Low,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // If the file has a main guard, it's fine
        if (MainGuardRegex().IsMatch(context.Content))
            yield break;

        // Check if there are top-level function calls that would execute on import
        var calls = TopLevelCallRegex().Matches(context.Content);
        if (calls.Count == 0)
            yield break;

        var firstCall = calls[0];
        var line = GetLineNumber(context.Content, firstCall.Index);

        yield return new Finding(
            RuleId: "RWS-PY-015",
            Title: "Top-level code without __main__ guard",
            Severity: Severity.Low,
            File: context.RelativePath,
            Line: line,
            Why: "Code at the top level of a module runs immediately when the file is imported. This causes side effects in automated grading scripts and prevents safe reuse of your functions.",
            Fix: "Wrap top-level logic in: if __name__ == '__main__': main()"
        );
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
