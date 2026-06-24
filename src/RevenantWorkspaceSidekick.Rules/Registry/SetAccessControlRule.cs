using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Registry;

/// <summary>RWS-REG-004: RegistryKey.SetAccessControl on HKLM/HKCR.</summary>
public sealed class SetAccessControlRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-REG-004",
        Title: "RegistryKey.SetAccessControl on protected hive",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            string? methodName;
            ExpressionSyntax? receiver;

            switch (invocation.Expression)
            {
                case MemberAccessExpressionSyntax m when m.Name.Identifier.Text == "SetAccessControl":
                    methodName = m.Name.Identifier.Text;
                    receiver = m.Expression;
                    break;
                case MemberBindingExpressionSyntax mb when mb.Name.Identifier.Text == "SetAccessControl"
                     && invocation.Parent is ConditionalAccessExpressionSyntax cond:
                    methodName = mb.Name.Identifier.Text;
                    receiver = cond.Expression;
                    break;
                default:
                    continue;
            }

            if (!HklmWriteRule.ChainContainsHklm(receiver) &&
                !HklmWriteRule.EnclosingMethodReferencesHklm(invocation))
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-REG-004",
                Title: "RegistryKey.SetAccessControl on HKLM/HKCR key",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "Modifying ACLs on HKLM or HKCR registry keys requires elevation and can create privilege-escalation opportunities if done incorrectly. AI-generated code often calls SetAccessControl to 'fix' access-denied errors without understanding the security implications.",
                Fix: "Do not grant broad permissions on system registry keys. Use a UAC-elevated installer to set required ACLs at install time. If runtime access is needed, move the key to HKCU."
            );
        }
    }
}
