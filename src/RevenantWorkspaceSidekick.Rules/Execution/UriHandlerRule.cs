using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Execution;

/// <summary>RWS-EXEC-004: Custom URI command registration pattern via registry.</summary>
public sealed class UriHandlerRule : IRule
{
    private static readonly HashSet<string> UriHandlerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "shell\\open\\command",
        "shell/open/command",
        "URL Protocol",
        "open\\command",
        "open/command"
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EXEC-004",
        Title: "Custom URI command registration pattern detected",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "SetValue" })
                continue;

            var args = invocation.ArgumentList.Arguments;
            var hasUriPattern = args.Any(a =>
            {
                if (a.Expression is not LiteralExpressionSyntax lit) return false;
                var value = lit.Token.ValueText;
                return UriHandlerKeywords.Any(kw =>
                    value.Contains(kw, StringComparison.OrdinalIgnoreCase));
            });

            if (!hasUriPattern)
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-EXEC-004",
                Title: "Custom URI handler registration via registry",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Registering a custom URI scheme handler that launches an executable allows external websites or apps to invoke your app with arbitrary arguments. Without strict argument validation this becomes a remote code execution vector.",
                Fix: "Validate all arguments received through custom URI schemes before using them. Use an allow-list for accepted actions. Never pass URI arguments directly to Process.Start or shell execution."
            );
        }
    }
}
