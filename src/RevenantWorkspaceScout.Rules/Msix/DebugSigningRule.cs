using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Msix;

/// <summary>RWS-MSIX-003: Suspicious test/debug signing artifact in manifest.</summary>
public sealed partial class DebugSigningRule : IRule
{
    [GeneratedRegex(@"CN=(Test|Debug|Self|[0-9A-Fa-f]{8})", RegexOptions.IgnoreCase)]
    private static partial Regex DebugCertPattern();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-MSIX-003",
        Title: "Suspicious test/debug signing artifact",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".appxmanifest"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var element in doc.Descendants())
        {
            if (element.Name.LocalName != "Identity")
                continue;

            var publisher = element.Attribute("Publisher")?.Value;
            if (publisher is null)
                continue;

            if (!DebugCertPattern().IsMatch(publisher))
                continue;

            yield return new Finding(
                RuleId: "RWS-MSIX-003",
                Title: "Test/debug certificate publisher in manifest",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: null,
                Why: $"The Publisher attribute '{publisher}' looks like a test or self-signed certificate. Shipping with a debug cert means your package cannot be installed on standard Windows configurations.",
                Fix: "Replace the Publisher value with your real code-signing certificate's subject name before shipping."
            );
        }
    }
}
