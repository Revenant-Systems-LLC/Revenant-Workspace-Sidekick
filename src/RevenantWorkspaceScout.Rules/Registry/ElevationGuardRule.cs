using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Registry;

/// <summary>RWS-REG-003: HKLM write without a visible elevation guard in the enclosing method.</summary>
public sealed class ElevationGuardRule : IRule
{
    private static readonly HashSet<string> ElevationGuardIdentifiers = new(StringComparer.Ordinal)
    {
        "IsInRole", "WindowsIdentity", "IsElevated", "RunAs", "WindowsPrincipal"
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-REG-003",
        Title: "Elevation-sensitive write without elevation guard",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var propName = access.Name.Identifier.Text;
            if (propName is not ("LocalMachine" or "ClassesRoot"))
                continue;

            if (access.Expression is not IdentifierNameSyntax { Identifier.Text: "Registry" })
                continue;

            // Walk up to the enclosing method and look for elevation guard identifiers
            var enclosingMethod = access.Ancestors()
                .FirstOrDefault(a => a is MethodDeclarationSyntax or LocalFunctionStatementSyntax);

            if (enclosingMethod is null)
                continue;

            var identifiers = enclosingMethod
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(i => i.Identifier.Text)
                .ToHashSet();

            if (identifiers.Overlaps(ElevationGuardIdentifiers))
                continue;

            var line = access.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-REG-003",
                Title: "HKLM/HKCR write with no elevation guard",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "This code writes to a protected registry hive without any visible elevation check. At runtime this will silently fail or throw an UnauthorizedAccessException for standard users.",
                Fix: "Add an elevation check before writing: use WindowsIdentity.GetCurrent() with WindowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator), or gate the write behind a UAC-aware code path."
            );
        }
    }
}
