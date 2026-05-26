using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.TypeScript;

/// <summary>RSH-TS-005: TS/JS HTML Injection.</summary>
public sealed partial class TsReactHtmlInjectionRule : IRule
{
    [GeneratedRegex(@"\bdangerouslySetInnerHTML\b")]
    private static partial Regex ReactHtmlRegex();

    [GeneratedRegex(@"\bbypassSecurityTrustHtml\b")]
    private static partial Regex AngularHtmlRegex();

    [GeneratedRegex(@"\b\.innerHTML\s*=\s*(.*?)\b")]
    private static partial Regex InnerHtmlRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-TS-005",
        Title: "HTML Injection risk",
        DefaultSeverity: Severity.High,
        FileExtensions: [".ts", ".tsx", ".js", ".jsx"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // dangerouslySetInnerHTML
        foreach (Match match in ReactHtmlRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-005",
                Title: "Use of dangerouslySetInnerHTML in React",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Using 'dangerouslySetInnerHTML' bypasses React's default XSS protections. If the content contains unescaped user input, it allows attackers to inject malicious scripts into the page (Cross-Site Scripting).",
                Fix: "Ensure all inputs passed to dangerouslySetInnerHTML are sanitized using DOMPurify, or use standard React children elements to render text safely."
            );
        }

        // bypassSecurityTrustHtml
        foreach (Match match in AngularHtmlRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-005",
                Title: "Use of bypassSecurityTrustHtml in Angular",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Bypassing Angular's built-in sanitizer disables automatic XSS protection, exposing the application to client-side script injection.",
                Fix: "Use Angular's standard DomSanitizer.sanitize() method, or sanitize the HTML content before bypassing security."
            );
        }

        // innerHTML
        foreach (Match match in InnerHtmlRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-TS-005",
                Title: "Direct write to innerHTML",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Assigning strings containing user inputs directly to '.innerHTML' allows DOM-based Cross-Site Scripting (XSS) attacks.",
                Fix: "Use '.textContent' or '.innerText' instead to automatically escape inputs. If HTML rendering is required, run inputs through a sanitizer library like DOMPurify first."
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
