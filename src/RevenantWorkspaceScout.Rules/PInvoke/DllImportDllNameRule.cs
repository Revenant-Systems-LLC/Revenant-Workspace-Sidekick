using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.PInvoke;

/// <summary>
/// RWS-PINVOKE-002: [DllImport] where the DLL name argument is not a string literal.
/// A computed or variable DLL name enables DLL hijacking via runtime path resolution.
/// </summary>
public sealed class DllImportDllNameRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PINVOKE-002",
        Title: "P/Invoke with non-literal DLL name",
        DefaultSeverity: Severity.High,
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

            var firstArg = attribute.ArgumentList?.Arguments
                .FirstOrDefault(a => a.NameEquals is null);

            if (firstArg is null)
                continue;

            if (firstArg.Expression is LiteralExpressionSyntax)
                continue;

            var line = attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-PINVOKE-002",
                Title: "P/Invoke with computed DLL name",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "A non-literal DLL name in DllImport is resolved at runtime using the Windows DLL search order. If any part of the path or name is derived from configuration, environment variables, or user input, an attacker who can write to a searched directory can substitute a malicious DLL.",
                Fix: "Use a hard-coded string literal for the DLL name in DllImport. If the DLL path must vary, use LoadLibrary with a fully-qualified absolute path validated against a known-safe location."
            );
        }
    }

    private static bool IsDllImport(AttributeSyntax attr) =>
        attr.Name is IdentifierNameSyntax { Identifier.Text: "DllImport" }
        or QualifiedNameSyntax { Right.Identifier.Text: "DllImport" };
}
