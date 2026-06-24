using System.Xml.Linq;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Msix;

/// <summary>RWS-MSIX-001: Over-broad package capability.</summary>
public sealed class MsixCapabilityRule : IRule
{
    private static readonly HashSet<string> SuspiciousCapabilities = new(StringComparer.OrdinalIgnoreCase)
    {
        "broadFileSystemAccess",
        "internetClientServer",
        "privateNetworkClientServer",
        "documentsLibrary",
        "enterpriseAuthentication",
        "allJoyn"
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-MSIX-001",
        Title: "Over-broad package capability",
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
            if (name is null || !SuspiciousCapabilities.Contains(name))
                continue;

            yield return new Finding(
                RuleId: "RWS-MSIX-001",
                Title: $"Over-broad capability declared: {name}",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: null,
                Why: $"The capability '{name}' grants broad system access. AI-generated manifests frequently include capabilities copied from examples without verifying they are actually required.",
                Fix: $"Remove '{name}' from your manifest unless your app genuinely requires it. Request the minimum capabilities needed."
            );
        }
    }
}
