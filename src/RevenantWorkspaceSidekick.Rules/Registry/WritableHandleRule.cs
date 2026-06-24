using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Registry;

/// <summary>RWS-REG-002: Writable registry handle opened against a protected hive.</summary>
public sealed class WritableHandleRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-REG-002",
        Title: "Writable registry handle against protected hive",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "OpenSubKey" })
                continue;

            var args = invocation.ArgumentList.Arguments;
            var hasWritableTrue = false;

            // Named argument: writable: true
            foreach (var arg in args)
            {
                if (arg.NameColon?.Name.Identifier.Text == "writable" &&
                    arg.Expression is LiteralExpressionSyntax { Token.ValueText: "True" or "true" })
                {
                    hasWritableTrue = true;
                    break;
                }
            }

            // Positional second arg = true
            if (!hasWritableTrue && args.Count >= 2 &&
                args[1].Expression is LiteralExpressionSyntax { Token.ValueText: "True" or "true" })
            {
                hasWritableTrue = true;
            }

            if (!hasWritableTrue)
                continue;

            // Only flag when we can see HKLM/HKCR in the receiver chain.
            // HKCU writable opens are safe for standard users.
            if (invocation.Expression is MemberAccessExpressionSyntax receiver &&
                !HklmWriteRule.ChainContainsHklm(receiver.Expression))
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-REG-002",
                Title: "OpenSubKey called with writable: true",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Opening a registry key as writable on a protected hive (HKLM/HKCR) will fail or throw without elevation. AI-generated code commonly does this unconditionally.",
                Fix: "Only open registry keys as writable when you have confirmed elevation. Prefer read-only access (writable: false) and perform writes through the installer."
            );
        }
    }
}
