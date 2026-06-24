using System.Xml.Linq;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Msix;

/// <summary>RWS-MSIX-002: runFullTrust capability enabled.</summary>
public sealed class RunFullTrustRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-MSIX-002",
        Title: "runFullTrust capability enabled",
        DefaultSeverity: Severity.High,
        FileExtensions: [".appxmanifest"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var element in doc.Descendants())
        {
            if (element.Name.LocalName != "Capability")
                continue;

            var name = element.Attribute("Name")?.Value;
            if (!string.Equals(name, "runFullTrust", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return new Finding(
                RuleId: "RWS-MSIX-002",
                Title: "runFullTrust capability is enabled",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: null,
                Why: "runFullTrust bypasses the MSIX sandbox and grants the app the same privileges as the installing user. This is often unnecessary for desktop utilities and is a common AI-generated manifest mistake.",
                Fix: "Remove runFullTrust unless your app genuinely requires full-trust execution. Prefer sandboxed capabilities where possible."
            );
        }
    }
}
