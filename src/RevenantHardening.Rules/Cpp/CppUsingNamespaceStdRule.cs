using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Cpp;

/// <summary>RSH-CPP-005: Using 'using namespace std;' in header files.</summary>
public sealed partial class CppUsingNamespaceStdRule : IRule
{
    [GeneratedRegex(@"^\s*using\s+namespace\s+std\s*;", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex UsingNamespaceStdRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-CPP-005",
        Title: "'using namespace std' in header file",
        DefaultSeverity: Severity.High,
        FileExtensions: [".h", ".hpp"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in UsingNamespaceStdRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-CPP-005",
                Title: "'using namespace std;' in header file",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "'using namespace std' in a header pollutes the global namespace for every file that includes it, causing hard-to-diagnose name collisions.",
                Fix: "Use explicit std:: qualification (e.g., std::string, std::vector) or limit 'using namespace std;' to .cpp source files only."
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
