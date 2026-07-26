using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Student;

/// <summary>RSH-EDU-BE-01: Detects Flask debug mode.</summary>
public sealed partial class StudentFlaskDebugRule : IRule
{
    [GeneratedRegex(@"debug\s*=\s*True", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FlaskDebugRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-EDU-BE-01",
        Title: "Hardcoded Flask Debug Mode",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in FlaskDebugRegex().Matches(context.Content))
        {
            var lineText = GetLineText(context.Content, match.Index);
            if (lineText.TrimStart().StartsWith('#'))
                continue;

            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Running a web application in debug mode exposes sensitive server information, stack traces, and can even allow remote code execution through the Werkzeug debugger console.",
                Fix: "Use environment variables to toggle debug mode (e.g., app.run(debug=os.environ.get('FLASK_DEBUG') == '1')). Never hardcode it to True."
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
