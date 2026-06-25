using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Acl;

/// <summary>
/// RWS-ACL-001: Directory.SetAccessControl or File.SetAccessControl call detected.
/// Modifying filesystem ACLs programmatically is rarely necessary in desktop apps
/// and is a common AI-generated antipattern.
/// </summary>
public sealed class FileAclRule : IRule
{
    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-ACL-001",
        Title: "Filesystem ACL modification detected",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            bool isSetAccessControl = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text == "SetAccessControl",
                MemberBindingExpressionSyntax mb => mb.Name.Identifier.Text == "SetAccessControl",
                _ => false
            };

            if (!isSetAccessControl)
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-ACL-001",
                Title: "ACL modified via SetAccessControl",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: line,
                Why: "Programmatically modifying ACLs can inadvertently grant overly broad permissions, expose sensitive files to other users, or create persistent privilege paths. AI-generated code often calls SetAccessControl to work around access-denied errors without understanding the security impact.",
                Fix: "Set required permissions during installation (NSIS, WiX, MSIX) rather than at runtime. If runtime ACL modification is necessary, use the principle of least privilege and explicitly log the change."
            );
        }
    }
}
