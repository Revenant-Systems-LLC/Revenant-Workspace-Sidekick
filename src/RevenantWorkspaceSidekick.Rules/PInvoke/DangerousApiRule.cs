using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.PInvoke;

/// <summary>
/// RWS-PINVOKE-003: [DllImport] of a known privilege-escalation or impersonation Win32 API.
/// </summary>
public sealed class DangerousApiRule : IRule
{
    private static readonly HashSet<string> DangerousApis = new(StringComparer.OrdinalIgnoreCase)
    {
        // Token / privilege manipulation
        "AdjustTokenPrivileges",
        "RtlAdjustPrivilege",
        "NtAdjustPrivilegesToken",
        "OpenProcessToken",
        "DuplicateTokenEx",
        "DuplicateToken",

        // Impersonation
        "ImpersonateLoggedOnUser",
        "ImpersonateNamedPipeClient",
        "ImpersonateSelf",
        "SetThreadToken",
        "RevertToSelf",

        // Process creation with alternate identity
        "CreateProcessWithTokenW",
        "CreateProcessAsUserW",
        "CreateProcessAsUserA",
        "CreateProcessWithLogonW",
        "LogonUser",
        "LogonUserW",
        "LogonUserA",
        "LogonUserEx",
        "LogonUserExW",
        "LogonUserExExW",
    };

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PINVOKE-003",
        Title: "P/Invoke of privilege-escalation or impersonation API",
        DefaultSeverity: Severity.High,
        FileExtensions: [".cs"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var tree = CSharpSyntaxTree.ParseText(context.Content);
        var root = tree.GetRoot();

        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            if (!IsDllImport(attribute))
                continue;

            // Method name and explicit EntryPoint (if set) are both checked
            var method = attribute.Parent?.Parent as MethodDeclarationSyntax;
            if (method is null)
                continue;

            var methodName = method.Identifier.Text;
            var entryPoint = GetEntryPoint(attribute);
            var apiName = entryPoint ?? methodName;

            if (!DangerousApis.Contains(apiName))
                continue;

            var line = attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new Finding(
                RuleId: "RWS-PINVOKE-003",
                Title: $"P/Invoke of sensitive Win32 API: {apiName}",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: $"{apiName} is a Windows API used for privilege manipulation, token impersonation, or elevated process creation. Incorrect use can create privilege escalation vulnerabilities or allow a lower-privileged caller to act as a higher-privileged user.",
                Fix: "Ensure this API call is strictly necessary and guarded by appropriate authorization checks. Prefer .NET-managed equivalents (WindowsIdentity.RunImpersonated, etc.) where available. Document why the P/Invoke is required."
            );
        }
    }

    private static bool IsDllImport(AttributeSyntax attr) =>
        attr.Name is IdentifierNameSyntax { Identifier.Text: "DllImport" }
        or QualifiedNameSyntax { Right.Identifier.Text: "DllImport" };

    private static string? GetEntryPoint(AttributeSyntax attr)
    {
        if (attr.ArgumentList is null) return null;
        var ep = attr.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "EntryPoint");
        return (ep?.Expression as LiteralExpressionSyntax)?.Token.ValueText;
    }
}
