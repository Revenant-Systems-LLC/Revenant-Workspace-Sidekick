using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Xaml;

/// <summary>
/// RWS-XAML-002: ResourceDictionary.Source set to a non-literal URI (C#), or to a
/// binding/dynamic-resource expression in XAML.
/// </summary>
public sealed class ResourceDictionaryRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-XAML-002",
        Title: "ResourceDictionary.Source from non-literal path",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs", ".xaml"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var ext = Path.GetExtension(context.Path);
        return ext.Equals(".xaml", StringComparison.OrdinalIgnoreCase)
            ? AnalyzeXaml(context)
            : AnalyzeCsharp(context);
    }

    private static IEnumerable<Finding> AnalyzeCsharp(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            // Match: <something>.Source = <expr>
            if (assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Source" })
                continue;

            // Safe: new Uri("<literal>") or new Uri("<literal>", UriKind.Relative)
            if (IsLiteralUriCreation(assignment.Right))
                continue;

            // Skip literal string assignments (uncommon but possible)
            if (assignment.Right is LiteralExpressionSyntax)
                continue;

            var line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-XAML-002",
                Title: "ResourceDictionary.Source assigned from non-literal path",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Loading a ResourceDictionary from a user-controlled or computed path loads arbitrary XAML, which can instantiate dangerous objects. This is equivalent to dynamic XAML injection.",
                Fix: "Use pack:// URIs with hard-coded assembly resource paths. Never build the Source URI from user input or configuration values."
            );
        }
    }

    private static bool IsLiteralUriCreation(ExpressionSyntax expr)
    {
        if (expr is not ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "Uri" } } oc)
            return false;
        var firstArg = oc.ArgumentList?.Arguments.FirstOrDefault();
        return firstArg?.Expression is LiteralExpressionSyntax;
    }

    private static IEnumerable<Finding> AnalyzeXaml(FileContext context)
    {
        XDocument doc;
        try { doc = XDocument.Parse(context.Content); }
        catch { yield break; }

        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "ResourceDictionary"))
        {
            var src = el.Attribute("Source")?.Value;
            if (src == null) continue;

            // Flag Source values that look like binding or dynamic-resource expressions
            if (src.StartsWith("{Binding", StringComparison.OrdinalIgnoreCase) ||
                src.StartsWith("{DynamicResource", StringComparison.OrdinalIgnoreCase) ||
                src.StartsWith("{StaticResource", StringComparison.OrdinalIgnoreCase))
            {
                var approxLine = FindLineTo(context.Content, "ResourceDictionary");
                yield return new Finding(
                    RuleId: "RWS-XAML-002",
                    Title: "ResourceDictionary Source uses a binding expression",
                    Severity: Severity.High,
                    File: context.RelativePath,
                    Line: approxLine,
                    Why: "A ResourceDictionary whose Source is a binding expression loads XAML from a runtime-determined location. If that location is attacker-influenced, this enables XAML injection.",
                    Fix: "Use a hard-coded pack:// URI as the Source. Apply theming by switching between known-safe static resource dictionaries, not by binding Source to a dynamic value."
                );
            }
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
