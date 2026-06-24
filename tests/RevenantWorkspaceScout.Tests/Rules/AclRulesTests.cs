using RevenantWorkspaceScout.Core.Models;
using RevenantWorkspaceScout.Rules.Acl;
using Xunit;

namespace RevenantWorkspaceScout.Tests.Rules;

public class AclRulesTests
{
    // RWS-ACL-001

    [Fact]
    public void FileAclRule_Triggers_OnDirectorySetAccessControl()
    {
        var rule = new FileAclRule();
        var ctx = Cs("new DirectoryInfo(path).SetAccessControl(security);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-ACL-001", findings[0].RuleId);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void FileAclRule_Triggers_OnFileSetAccessControl()
    {
        var rule = new FileAclRule();
        var ctx = Cs("new FileInfo(path).SetAccessControl(fileSecurity);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-ACL-001", findings[0].RuleId);
    }

    [Fact]
    public void FileAclRule_Triggers_OnRegistrySetAccessControl()
    {
        var rule = new FileAclRule();
        var ctx = Cs("""
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\App", true);
            key?.SetAccessControl(new RegistrySecurity());
            """);

        // ACL-001 fires on any SetAccessControl call; REG-004 also fires (both rules cover registry)
        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-ACL-001", findings[0].RuleId);
    }

    // RWS-ACL-002

    [Fact]
    public void OpenAccessRuleRule_Triggers_OnEveryoneRule()
    {
        var rule = new OpenAccessRuleRule();
        var ctx = Cs("""
            var r = new FileSystemAccessRule("Everyone",
                FileSystemRights.FullControl, AccessControlType.Allow);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-ACL-002", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void OpenAccessRuleRule_Triggers_OnUsersRule()
    {
        var rule = new OpenAccessRuleRule();
        var ctx = Cs("""
            var r = new FileSystemAccessRule("Users",
                FileSystemRights.ReadAndExecute, AccessControlType.Allow);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-ACL-002", findings[0].RuleId);
    }

    [Fact]
    public void OpenAccessRuleRule_DoesNotTrigger_OnServiceAccount()
    {
        var rule = new OpenAccessRuleRule();
        var ctx = Cs("""
            var r = new FileSystemAccessRule("NT SERVICE\\MyApp",
                FileSystemRights.ReadAndExecute, AccessControlType.Allow);
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    private static FileContext Cs(string code) =>
        new("Test.cs", "Test.cs",
            "using System.IO;\nusing System.Security.AccessControl;\nusing Microsoft.Win32;\n" + code);
}
