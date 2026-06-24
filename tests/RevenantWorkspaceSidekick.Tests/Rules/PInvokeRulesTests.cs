using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Rules.PInvoke;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests.Rules;

public class PInvokeRulesTests
{
    // RWS-PINVOKE-001

    [Fact]
    public void DllImportCharSetRule_Triggers_OnStringParamWithoutUnicode()
    {
        var rule = new DllImportCharSetRule();
        var ctx = Cs("""
            [DllImport("kernel32.dll")]
            static extern bool CreateDirectory(string path, IntPtr sec);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PINVOKE-001", findings[0].RuleId);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void DllImportCharSetRule_DoesNotTrigger_WithUnicode()
    {
        var rule = new DllImportCharSetRule();
        var ctx = Cs("""
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            static extern bool CreateDirectoryW(string path, IntPtr sec);
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void DllImportCharSetRule_DoesNotTrigger_WithoutStringParam()
    {
        var rule = new DllImportCharSetRule();
        var ctx = Cs("""
            [DllImport("kernel32.dll")]
            static extern IntPtr GetCurrentProcess();
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PINVOKE-002

    [Fact]
    public void DllImportDllNameRule_Triggers_OnNonLiteralDllName()
    {
        var rule = new DllImportDllNameRule();
        var ctx = Cs("""
            private const string DllName = "kernel32.dll";
            [DllImport(DllName)]
            static extern IntPtr GetCurrentProcess();
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PINVOKE-002", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void DllImportDllNameRule_DoesNotTrigger_OnLiteralDllName()
    {
        var rule = new DllImportDllNameRule();
        var ctx = Cs("""
            [DllImport("kernel32.dll")]
            static extern IntPtr GetCurrentProcess();
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PINVOKE-003

    [Fact]
    public void DangerousApiRule_Triggers_OnAdjustTokenPrivileges()
    {
        var rule = new DangerousApiRule();
        var ctx = Cs("""
            [DllImport("advapi32.dll", SetLastError = true)]
            static extern bool AdjustTokenPrivileges(IntPtr token, bool disable,
                ref int newState, int len, IntPtr prev, IntPtr ret);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PINVOKE-003", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void DangerousApiRule_Triggers_OnImpersonateLoggedOnUser()
    {
        var rule = new DangerousApiRule();
        var ctx = Cs("""
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
            static extern bool ImpersonateLoggedOnUser(IntPtr hToken);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PINVOKE-003", findings[0].RuleId);
    }

    [Fact]
    public void DangerousApiRule_Triggers_OnEntryPointOverride()
    {
        var rule = new DangerousApiRule();
        var ctx = Cs("""
            [DllImport("advapi32.dll", EntryPoint = "LogonUserW")]
            static extern bool LogonUser(string user, string domain, string pass,
                int type, int provider, out IntPtr token);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PINVOKE-003", findings[0].RuleId);
    }

    [Fact]
    public void DangerousApiRule_DoesNotTrigger_OnSafeApi()
    {
        var rule = new DangerousApiRule();
        var ctx = Cs("""
            [DllImport("kernel32.dll")]
            static extern IntPtr GetCurrentProcess();
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    private static FileContext Cs(string code) =>
        new("Test.cs", "Test.cs",
            "using System.Runtime.InteropServices;\n" +
            "public static class NativeWrapper {\n" + code + "\n}");
}
