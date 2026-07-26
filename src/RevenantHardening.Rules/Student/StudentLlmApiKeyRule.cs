using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Student;

/// <summary>RSH-EDU-AI-01: Detects hardcoded LLM API keys.</summary>
public sealed partial class StudentLlmApiKeyRule : IRule
{
    [GeneratedRegex(@"(?:OPENAI_API_KEY|ANTHROPIC_API_KEY|GEMINI_API_KEY)\s*[:=]\s*[""'](sk-[A-Za-z0-9_-]+|AIzaSy[A-Za-z0-9_-]+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-EDU-AI-01",
        Title: "Hardcoded LLM API Key",
        DefaultSeverity: Severity.High,
        FileExtensions: [".py", ".ts", ".js", ".tsx", ".jsx", ".json", ".env.example"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        foreach (Match match in ApiKeyRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: Metadata.Id,
                Title: Metadata.Title,
                Severity: Metadata.DefaultSeverity,
                File: context.RelativePath,
                Line: line,
                Why: "Hardcoding AI API keys directly into source code guarantees they will eventually be pushed to a repository and stolen by scrapers.",
                Fix: "Move the key to a local .env file and load it using python-dotenv or process.env. Ensure .env is in your .gitignore!"
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
