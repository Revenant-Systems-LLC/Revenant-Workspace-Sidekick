using System.Xml.Linq;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Xaml;

/// <summary>RWS-XAML-003: x:Code element detected in XAML — inline C# embedded in markup.</summary>
public sealed class XamlCodeElementRule : IRule
{
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-XAML-003",
        Title: "x:Code element in XAML",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".xaml"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var el in doc.Descendants()
            .Where(e => e.Name.LocalName == "Code" &&
                        e.Name.NamespaceName == XamlNamespace))
        {
            var approxLine = FindLineTo(context.Content, "x:Code");
            yield return new Finding(
                RuleId: "RWS-XAML-003",
                Title: "Inline C# code via x:Code in XAML",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: approxLine,
                Why: "x:Code embeds executable C# directly in a XAML file, bypassing normal code-review workflows and making security audits harder. AI assistants occasionally generate this pattern when they cannot resolve a code-behind reference.",
                Fix: "Move the logic to a code-behind file (.xaml.cs) or a ViewModel. Remove the x:Code block entirely."
            );
        }
    }

    private static int? FindLineTo(string content, string keyword)
    {
        var idx = content.IndexOf(keyword, StringComparison.Ordinal);
        if (idx < 0) return null;
        var line = 1;
        for (var i = 0; i < idx; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
