using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.PInvoke;

/// <summary>
/// RWS-PINVOKE-001: [DllImport] without CharSet=Unicode where the extern function has
/// string or StringBuilder parameters. The default CharSet.None causes platform-dependent
/// marshaling that silently corrupts strings on Unicode-only systems.
/// </summary>
public sealed class DllImportCharSetRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PINVOKE-001",
        Title: "P/Invoke missing CharSet=Unicode on string-parameter function",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            if (!IsDllImport(attribute))
                continue;

            if (HasUnicodeCharSet(attribute))
                continue;

            var method = attribute.Parent?.Parent as MethodDeclarationSyntax;
            if (method is null)
                continue;

            if (!HasStringParameter(method))
                continue;

            var line = attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-PINVOKE-001",
                Title: "P/Invoke with string parameters missing CharSet=Unicode",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "The default CharSet for DllImport is CharSet.None (equivalent to CharSet.Ansi on most platforms). Calling a Unicode Win32 API without CharSet.Unicode silently marshals strings as ANSI, corrupting non-ASCII characters and potentially causing security-relevant data truncation.",
                Fix: "Add CharSet = CharSet.Unicode to the DllImport attribute, or better yet use the W-suffix Unicode variant of the Win32 function explicitly (e.g., CreateFileW instead of CreateFile)."
            );
        }
    }

    private static bool IsDllImport(AttributeSyntax attr) =>
        attr.Name is IdentifierNameSyntax { Identifier.Text: "DllImport" }
        or QualifiedNameSyntax { Right.Identifier.Text: "DllImport" };

    private static bool HasUnicodeCharSet(AttributeSyntax attr)
    {
        if (attr.ArgumentList is null) return false;
        return attr.ArgumentList.Arguments.Any(a =>
            a.NameEquals?.Name.Identifier.Text == "CharSet" &&
            a.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Unicode" });
    }

    private static bool HasStringParameter(MethodDeclarationSyntax method) =>
        method.ParameterList.Parameters.Any(p =>
        {
            var typeName = p.Type?.ToString();
            return typeName is "string" or "String" or "StringBuilder";
        });
}
