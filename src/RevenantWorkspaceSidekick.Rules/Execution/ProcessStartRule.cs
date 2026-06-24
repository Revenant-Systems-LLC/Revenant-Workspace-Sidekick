using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Execution;

/// <summary>
/// RWS-EXEC-001: Process.Start with non-literal arguments.
/// Conservative heuristic: flags any call where an argument is not a compile-time string literal,
/// treating it as potentially user-influenced. False positives are expected on internal paths.
/// Taint tracking will be added in a future version.
/// </summary>
public sealed class ProcessStartRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-001",
        Title: "Process.Start with potentially user-influenced argument",
        DefaultSeverity: Severity.High,
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

            // Skip Process.Start(new ProcessStartInfo { ... }) — covered by RWS-EXEC-002
            if (args.Count == 1 && args[0].Expression is ObjectCreationExpressionSyntax)
                continue;

            // Skip interpolated strings and Format calls — covered at Critical by RWS-EXEC-005
            if (args.Any(a => ProcessStartInterpolationRule.IsRiskyStringConstruction(a.Expression)))
                continue;

            var hasNonLiteral = args.Any(a => a.Expression is not LiteralExpressionSyntax);

            if (!hasNonLiteral)
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-EXEC-001",
                Title: "Process.Start with non-literal argument (potentially user-influenced)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "RWS-EXEC-001 uses a conservative heuristic: any non-literal argument to Process.Start is flagged as potentially user-influenced (CLI args, config, UI input). False positives on purely internal paths are expected; taint tracking will be added in a future version.",
                Fix: "Validate and sanitize all arguments before passing to Process.Start. Prefer explicit allow-lists over open-ended input. If the path is truly internal, consider extracting it to a string constant to suppress this finding."
            );
        }
    }

    private static bool IsProcessStart(InvocationExpressionSyntax inv) =>
        inv.Expression is MemberAccessExpressionSyntax
        {
            Name.Identifier.Text: "Start",
            Expression: IdentifierNameSyntax { Identifier.Text: "Process" }
        };
}
