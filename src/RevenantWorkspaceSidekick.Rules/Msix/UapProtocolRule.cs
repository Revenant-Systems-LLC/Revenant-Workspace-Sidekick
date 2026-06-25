using System.Xml.Linq;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Msix;

/// <summary>RWS-MSIX-004: Custom URI protocol registered in MSIX manifest.</summary>
public sealed class UapProtocolRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-MSIX-004",
        Title: "Custom URI protocol registered in MSIX manifest",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".appxmanifest"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var element in doc.Descendants().Where(e => e.Name.LocalName == "Protocol"))
        {
            var schemeName = element.Attribute("Name")?.Value ?? "<unnamed>";
            var approxLine = FindProtocolLine(context.Content);

            yield return new Finding(
                RuleId: "RWS-MSIX-004",
                Title: $"Custom URI scheme '{schemeName}' registered in manifest",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: approxLine,
                Why: "Registering a custom URI protocol allows any website or app to invoke your application with arbitrary arguments. Without strict validation of the URI arguments in your activation handler, this is a remote code execution surface.",
                Fix: "Implement an activation argument allow-list in your protocol activation handler. Never pass URI arguments directly to Process.Start or shell execution. Consider whether the protocol registration is necessary — if unused, remove it."
            );
        }
    }

    private static int? FindProtocolLine(string content)
    {
        var idx = content.IndexOf("Protocol", StringComparison.Ordinal);
        if (idx < 0) return null;
        var line = 1;
        for (var i = 0; i < idx; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
