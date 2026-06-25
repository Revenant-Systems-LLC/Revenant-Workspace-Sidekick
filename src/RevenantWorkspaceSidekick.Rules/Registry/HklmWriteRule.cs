using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Registry;

/// <summary>RWS-REG-001: HKLM or HKCR write detected.</summary>
public sealed class HklmWriteRule : IRule
{
    private static readonly HashSet<string> WriteMethods = new(StringComparer.Ordinal)
    {
        "SetValue", "CreateSubKey", "CreateSubKeyTree",
        "DeleteSubKey", "DeleteSubKeyTree", "DeleteValue"
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-REG-001",
        Title: "HKLM/HKCR registry write detected",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            // Normal call: receiver.SetValue(...)
            // Conditional: receiver?.SetValue(...) — expression is MemberBindingExpressionSyntax
            string? methodName;
            ExpressionSyntax? directReceiver;

            switch (invocation.Expression)
            {
                case MemberAccessExpressionSyntax m when WriteMethods.Contains(m.Name.Identifier.Text):
                    methodName = m.Name.Identifier.Text;
                    directReceiver = m.Expression;
                    break;
                case MemberBindingExpressionSyntax mb when WriteMethods.Contains(mb.Name.Identifier.Text)
                     && invocation.Parent is ConditionalAccessExpressionSyntax condParent:
                    methodName = mb.Name.Identifier.Text;
                    directReceiver = condParent.Expression;
                    break;
                default:
                    continue;
            }

            // Direct chain: Registry.LocalMachine.OpenSubKey(...).SetValue(...)
            // Two-step: var key = Registry.LocalMachine...; key.SetValue(...)
            //   — for two-step we check the enclosing method for an HKLM reference.
            if (!ChainContainsHklm(directReceiver) && !EnclosingMethodReferencesHklm(invocation))
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            yield return new Finding(
                RuleId: "RWS-REG-001",
                Title: $"Registry write to HKLM/HKCR via {methodName}",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Writing to HKLM or HKCR requires elevation. On standard user accounts this will fail or throw, and AI-generated code often does this without any error handling or elevation check.",
                Fix: "Move state to HKCU (HKEY_CURRENT_USER) where possible. If HKLM is required, add an explicit elevation check or use a UAC-aware installer to perform the write at install time."
            );
        }
    }

    internal static bool ChainContainsHklm(ExpressionSyntax expr) => expr switch
    {
        MemberAccessExpressionSyntax m =>
            (m.Name.Identifier.Text is "LocalMachine" or "ClassesRoot" &&
             m.Expression is IdentifierNameSyntax { Identifier.Text: "Registry" })
            || ChainContainsHklm(m.Expression),
        InvocationExpressionSyntax inv => ChainContainsHklm(inv.Expression),
        ConditionalAccessExpressionSyntax cond => ChainContainsHklm(cond.Expression),
        _ => false
    };

    internal static bool EnclosingMethodReferencesHklm(SyntaxNode node)
    {
        var method = node.Ancestors()
            .FirstOrDefault(a => a is MethodDeclarationSyntax or LocalFunctionStatementSyntax
                                   or AnonymousFunctionExpressionSyntax);
        if (method is null) return false;

        return method.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(a => a.Name.Identifier.Text is "LocalMachine" or "ClassesRoot" &&
                      a.Expression is IdentifierNameSyntax { Identifier.Text: "Registry" });
    }
}
