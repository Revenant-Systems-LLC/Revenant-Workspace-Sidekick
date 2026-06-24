using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Execution;

/// <summary>RWS-EXEC-002: UseShellExecute = true in a ProcessStartInfo.</summary>
public sealed class UseShellExecuteRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-002",
        Title: "UseShellExecute = true in risky context",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            var left = assignment.Left;
            string? propName = left switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            };

            if (propName != "UseShellExecute")
                continue;

            if (assignment.Right is not LiteralExpressionSyntax lit)
                continue;

            if (lit.Token.ValueText is not ("True" or "true"))
                continue;

            // Common safe pattern: new ProcessStartInfo { UseShellExecute = true, FileName = "<literal>" }
            // This is used to open URLs/documents in their default app — not a risky context.
            if (IsInLiteralFilenameInitializer(assignment))
                continue;

            var line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-EXEC-002",
                Title: "UseShellExecute = true detected",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "UseShellExecute = true passes the executable to the Windows shell for launch, enabling shell injection if any argument is derived from user input. AI-generated code commonly sets this without considering the risk.",
                Fix: "Set UseShellExecute = false and specify the executable path directly. Ensure all arguments are validated or come from trusted sources only."
            );
        }
    }

    private static bool IsInLiteralFilenameInitializer(AssignmentExpressionSyntax assignment)
    {
        var initializer = assignment.Ancestors()
            .OfType<InitializerExpressionSyntax>()
            .FirstOrDefault();

        if (initializer is null) return false;

        return initializer.Expressions
            .OfType<AssignmentExpressionSyntax>()
            .Any(a =>
                a.Left is IdentifierNameSyntax { Identifier.Text: "FileName" } &&
                a.Right is LiteralExpressionSyntax);
    }
}
