using RevenantWorkspaceScout.Core.Models;
using RevenantWorkspaceScout.Rules.Execution;
using Xunit;
#pragma warning disable CS8625 // null literal for non-nullable — intentional in test snippets

namespace RevenantWorkspaceScout.Tests.Rules;

public class ExecutionRulesTests
{
    // RWS-EXEC-001

    [Fact]
    public void ProcessStartRule_Triggers_OnVariableArgument()
    {
        var rule = new ProcessStartRule();
        var ctx = Cs("Process.Start(userInput);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-001", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void ProcessStartRule_Triggers_OnProcessStartInfoVariable()
    {
        var rule = new ProcessStartRule();
        var ctx = Cs("Process.Start(psi);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-001", findings[0].RuleId);
    }

    [Fact]
    public void ProcessStartRule_DoesNotTrigger_OnLiteralPath()
    {
        var rule = new ProcessStartRule();
        var ctx = Cs("Process.Start(\"notepad.exe\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void ProcessStartRule_DoesNotTrigger_OnInlineProcessStartInfo()
    {
        var rule = new ProcessStartRule();
        var ctx = Cs("Process.Start(new ProcessStartInfo { FileName = \"notepad.exe\", UseShellExecute = false });");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-EXEC-002

    [Fact]
    public void UseShellExecuteRule_Triggers_OnTrueAssignment()
    {
        var rule = new UseShellExecuteRule();
        var ctx = Cs("psi.UseShellExecute = true;");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-002", findings[0].RuleId);
    }

    [Fact]
    public void UseShellExecuteRule_DoesNotTrigger_OnFalseAssignment()
    {
        var rule = new UseShellExecuteRule();
        var ctx = Cs("psi.UseShellExecute = false;");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void UseShellExecuteRule_DoesNotTrigger_OnLiteralFilenameInitializer()
    {
        // Common safe pattern: open a URL in the default browser
        var rule = new UseShellExecuteRule();
        var ctx = Cs("var psi = new ProcessStartInfo { FileName = \"https://example.com\", UseShellExecute = true };");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void UseShellExecuteRule_Triggers_OnVariableFilenameInitializer()
    {
        var rule = new UseShellExecuteRule();
        var ctx = Cs("var psi = new ProcessStartInfo { FileName = userUrl, UseShellExecute = true };");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-002", findings[0].RuleId);
    }

    // RWS-EXEC-003

    [Fact]
    public void AssemblyLoadRule_Triggers_OnVariablePath()
    {
        var rule = new AssemblyLoadRule();
        var ctx = Cs("Assembly.LoadFrom(pluginPath);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-003", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void AssemblyLoadRule_Triggers_OnLoadFile()
    {
        var rule = new AssemblyLoadRule();
        var ctx = Cs("Assembly.LoadFile(path);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-003", findings[0].RuleId);
    }

    [Fact]
    public void AssemblyLoadRule_DoesNotTrigger_OnLiteralPath()
    {
        var rule = new AssemblyLoadRule();
        var ctx = Cs("Assembly.LoadFrom(\"MyPlugin.dll\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-EXEC-004

    [Fact]
    public void UriHandlerRule_Triggers_OnShellOpenCommandRegistration()
    {
        var rule = new UriHandlerRule();
        var ctx = Cs(@"Registry.SetValue(@""HKEY_CLASSES_ROOT\myapp\shell\open\command"", """", exePath);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-004", findings[0].RuleId);
    }

    [Fact]
    public void UriHandlerRule_Triggers_OnUrlProtocolRegistration()
    {
        var rule = new UriHandlerRule();
        var ctx = Cs(@"Registry.SetValue(@""HKEY_CLASSES_ROOT\myapp"", ""URL Protocol"", """");");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-004", findings[0].RuleId);
    }

    [Fact]
    public void UriHandlerRule_DoesNotTrigger_OnSafeSetValue()
    {
        var rule = new UriHandlerRule();
        var ctx = Cs(@"Registry.SetValue(@""HKEY_CURRENT_USER\SOFTWARE\MyApp"", ""Version"", ""1.0"");");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-EXEC-005

    [Fact]
    public void ProcessStartInterpolationRule_Triggers_OnInterpolatedFirstArg()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start($\"cmd /c {userCmd}\");");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-005", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void ProcessStartInterpolationRule_Triggers_OnInterpolatedSecondArg()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start(\"cmd.exe\", $\"/c {userInput}\");");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-005", findings[0].RuleId);
    }

    [Fact]
    public void ProcessStartInterpolationRule_Triggers_OnStringFormatArg()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start(string.Format(\"cmd /c {0}\", input));");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-005", findings[0].RuleId);
    }

    [Fact]
    public void ProcessStartInterpolationRule_Triggers_OnConcatWithVariable()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start(\"cmd /c \" + userCmd);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-EXEC-005", findings[0].RuleId);
    }

    [Fact]
    public void ProcessStartInterpolationRule_DoesNotTrigger_OnLiteralArgs()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start(\"notepad.exe\", \"file.txt\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void ProcessStartInterpolationRule_DoesNotTrigger_OnLiteralConcatOnly()
    {
        var rule = new ProcessStartInterpolationRule();
        var ctx = Cs("Process.Start(\"note\" + \"pad.exe\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void ProcessStartRule_DoesNotTrigger_OnInterpolatedArg_DeferredToExec005()
    {
        // EXEC-001 must NOT fire when EXEC-005 would fire, to avoid duplicate findings
        var rule = new ProcessStartRule();
        var ctx = Cs("Process.Start(\"cmd.exe\", $\"/c {userInput}\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    private static FileContext Cs(string code) =>
        new FileContext("test.cs", "test.cs",
            "using System.Diagnostics;\nusing System.Reflection;\nusing Microsoft.Win32;\n" + code);
}
