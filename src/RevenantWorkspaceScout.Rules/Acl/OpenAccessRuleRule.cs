using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Acl;

/// <summary>
/// RWS-ACL-002: FileSystemAccessRule constructed with a well-known overly-permissive identity
/// (Everyone, Users, Authenticated Users). These grants typically give all local users access.
/// </summary>
public sealed class OpenAccessRuleRule : IRule
{
    private static readonly HashSet<string> BroadIdentities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Everyone",
        "Users",
        "Authenticated Users",
        "World",
        "BUILTIN\\Users",
        "NT AUTHORITY\\Authenticated Users",
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-ACL-002",
        Title: "FileSystemAccessRule with broad identity (Everyone/Users)",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = creation.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                _ => null
            };

            if (typeName is not ("FileSystemAccessRule" or "RegistryAccessRule"))
                continue;

            var firstArg = creation.ArgumentList?.Arguments.FirstOrDefault();
            if (firstArg?.Expression is not LiteralExpressionSyntax lit)
                continue;

            if (!BroadIdentities.Contains(lit.Token.ValueText))
                continue;

            var line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-ACL-002",
                Title: $"{typeName} grants access to '{lit.Token.ValueText}'",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: $"Granting '{lit.Token.ValueText}' access to a filesystem path or registry key means every local user (or in some configurations, network users) can access that resource. This is a common AI-generated pattern that opens data disclosure and tampering risks.",
                Fix: "Grant access only to the specific service account or user identity that requires it. Use the application's own identity (ApplicationPoolIdentity, service account) rather than broad groups."
            );
        }
    }
}
