using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Execution;

/// <summary>RWS-EXEC-003: Assembly.LoadFrom/LoadFile with a non-literal path.</summary>
public sealed class AssemblyLoadRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-003",
        Title: "Assembly.LoadFrom/LoadFile with non-literal path",
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

            if (m.Name.Identifier.Text is not ("LoadFrom" or "LoadFile"))
                continue;

            if (m.Expression is not IdentifierNameSyntax { Identifier.Text: "Assembly" })
                continue;

            var args = invocation.ArgumentList.Arguments;
            if (args.Count == 0 || args[0].Expression is LiteralExpressionSyntax)
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-EXEC-003",
                Title: $"Assembly.{m.Name.Identifier.Text} with non-literal path",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Loading an assembly from a path derived from variables or external input enables a plugin-loading attack: an attacker who can write to the target path can execute arbitrary code in your process.",
                Fix: "Only load assemblies from known, validated paths. Prefer embedding resources or using signed-assembly verification before loading. Never load from user-controlled paths without strong integrity checks."
            );
        }
    }
}
