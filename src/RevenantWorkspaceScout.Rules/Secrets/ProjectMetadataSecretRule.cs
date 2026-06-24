using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Secrets;

/// <summary>RWS-SEC-003: Credential-like value in project metadata (.csproj, .props, .targets).</summary>
public sealed partial class ProjectMetadataSecretRule : IRule
{
    [GeneratedRegex(@"(?i)(api[_\-]?key|apikey|token|secret|password|passwd|pwd)\s*[=:]\s*[""']?[A-Za-z0-9+/\-_.]{12,}[""']?")]
    private static partial Regex GenericSecretPattern();

    [GeneratedRegex(@"ghp_[A-Za-z0-9]{36}")]
    private static partial Regex GithubPatPattern();

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}")]
    private static partial Regex AwsKeyPattern();

    private static readonly (Regex Pattern, string Label)[] InlinePatterns =
    [
        (GithubPatPattern(), "GitHub PAT"),
        (AwsKeyPattern(),    "AWS access key"),
    ];

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-SEC-003",
        Title: "Credential-like value in project metadata",
        DefaultSeverity: Severity.High,
        FileExtensions: [".csproj", ".props", ".targets"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // XML property value scan
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var element in doc.Descendants())
        {
            if (element.HasElements || string.IsNullOrWhiteSpace(element.Value))
                continue;

            var value = element.Value.Trim();
            var name = element.Name.LocalName;

            // Check for suspicious element names with non-trivial values
            if (GenericSecretPattern().IsMatch($"{name}={value}"))
            {
                var approxLine = GetApproxLine(context.Content, name, value);
                yield return new Finding(
                    RuleId: "RWS-SEC-003",
                    Title: $"Credential-like project property: <{name}>",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: approxLine,
                    Why: "Project files (.csproj, .props) are committed to source control. Secrets in MSBuild properties leak into build logs, NuGet package metadata, and CI system output.",
                    Fix: "Remove the credential from the project file. Use environment variables or a secrets file excluded from source control (e.g., a .user file in .gitignore).",
                    RedactedSnippet: SecretBlinder.Blind(value)
                );
                continue;
            }

            // Token-pattern scan of raw value
            foreach (var (pattern, label) in InlinePatterns)
            {
                if (pattern.IsMatch(value))
                {
                    var approxLine = GetApproxLine(context.Content, name, value);
                    yield return new Finding(
                        RuleId: "RWS-SEC-003",
                        Title: $"Hardcoded {label} in project property <{name}>",
                        Severity: Severity.High,
                        File: context.RelativePath,
                        Line: approxLine,
                        Why: "Project files (.csproj, .props) are committed to source control. Secrets in MSBuild properties leak into build logs, NuGet package metadata, and CI system output.",
                        Fix: "Remove the credential from the project file. Use environment variables or a secrets file excluded from source control.",
                        RedactedSnippet: SecretBlinder.Blind(value)
                    );
                }
            }
        }
    }

    private static int? GetApproxLine(string content, string elementName, string value)
    {
        var idx = content.IndexOf(value, StringComparison.Ordinal);
        if (idx < 0) return null;
        var line = 1;
        for (var i = 0; i < idx; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
