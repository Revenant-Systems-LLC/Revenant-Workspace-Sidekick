using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Xaml;

/// <summary>
/// RWS-XAML-001: XamlReader.Load or XamlReader.Parse with any argument — dynamic XAML parsing
/// can instantiate arbitrary .NET objects and execute code.
/// </summary>
public sealed class XamlReaderRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-XAML-001",
        Title: "Dynamic XAML parsing via XamlReader",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax m)
                continue;

            if (m.Name.Identifier.Text is not ("Load" or "Parse"))
                continue;

            if (m.Expression is not IdentifierNameSyntax { Identifier.Text: "XamlReader" })
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-XAML-001",
                Title: $"Dynamic XAML parsing via XamlReader.{m.Name.Identifier.Text}",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "XamlReader.Load and XamlReader.Parse execute XAML as code — any XAML fed from an untrusted source can instantiate arbitrary .NET objects, including those that run shell commands or invoke P/Invoke. This is effectively remote code execution.",
                Fix: "Never parse XAML from untrusted sources. If dynamic theming is required, use a strictly controlled allow-list of known-safe resource dictionaries. If you must parse user-supplied XAML, run it in a sandboxed AppDomain with restricted permissions."
            );
        }
    }
}
