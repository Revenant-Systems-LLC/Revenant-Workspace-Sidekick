using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Execution;

/// <summary>
/// RWS-EXEC-005: Process.Start where an argument is an interpolated string, string.Format call,
/// or a string concatenation that contains a non-literal operand. This is a narrower, higher-severity
/// companion to RWS-EXEC-001: interpolation/Format patterns are near-certain injection vectors.
/// </summary>
public sealed class ProcessStartInterpolationRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-005",
        Title: "Process.Start with interpolated/formatted string argument",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!IsProcessStart(invocation))
                continue;

            var args = invocation.ArgumentList.Arguments;

            // Skip Process.Start(new ProcessStartInfo{...}) — covered by RWS-EXEC-002
            if (args.Count == 1 && args[0].Expression is ObjectCreationExpressionSyntax)
                continue;

            if (!args.Any(a => IsRiskyStringConstruction(a.Expression)))
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-EXEC-005",
                Title: "Process.Start with interpolated/formatted string argument",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Constructing a process path or argument string via interpolation, string.Format, or concatenation is a command injection vector. Any dynamic component can be controlled by an attacker who influences the values being embedded.",
                Fix: "Use a fixed literal for the executable. Pass arguments as a separate, validated string. Never construct command-line strings dynamically from untrusted input."
            );
        }
    }

    private static bool IsProcessStart(InvocationExpressionSyntax inv) =>
        inv.Expression is MemberAccessExpressionSyntax
        {
            Name.Identifier.Text: "Start",
            Expression: IdentifierNameSyntax { Identifier.Text: "Process" }
        };

    internal static bool IsRiskyStringConstruction(ExpressionSyntax expr) => expr switch
    {
        InterpolatedStringExpressionSyntax => true,
        InvocationExpressionSyntax inv => IsStringFormatCall(inv),
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } bin =>
            ContainsNonLiteral(bin),
        _ => false
    };

    private static bool IsStringFormatCall(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is not MemberAccessExpressionSyntax m)
            return false;
        if (m.Name.Identifier.Text != "Format")
            return false;
        // `string` keyword parses as PredefinedTypeSyntax; `String` as IdentifierNameSyntax
        return m.Expression is PredefinedTypeSyntax { Keyword.ValueText: "string" }
            || m.Expression is IdentifierNameSyntax { Identifier.Text: "String" };
    }

    private static bool ContainsNonLiteral(ExpressionSyntax expr) => expr switch
    {
        LiteralExpressionSyntax => false,
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } bin =>
            ContainsNonLiteral(bin.Left) || ContainsNonLiteral(bin.Right),
        _ => true
    };
}
