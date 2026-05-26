using RevenantHardening.Core.Models;
using RevenantHardening.Rules.Java;
using Xunit;

namespace RevenantHardening.Tests.Rules;

public class JavaRulesTests
{
    private static FileContext Jv(string code) =>
        new("App.java", "App.java", code);

    // RSH-JV-001 (JavaCommandExecutionRule)

    [Fact]
    public void JavaCommandExecutionRule_Triggers_OnDynamicRuntimeExec()
    {
        var rule = new JavaCommandExecutionRule();
        var ctx = Jv("Runtime.getRuntime().exec(cmdVar + \" -la\")");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RSH-JV-001", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void JavaCommandExecutionRule_DoesNotTrigger_OnLiteralRuntimeExec()
    {
        var rule = new JavaCommandExecutionRule();
        var ctx = Jv("Runtime.getRuntime().exec(\"ls\")");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RSH-JV-002 (JavaInsecureDeserializationRule)

    [Fact]
    public void JavaInsecureDeserializationRule_Triggers_OnObjectInputStream()
    {
        var rule = new JavaInsecureDeserializationRule();
        var ctx = Jv("new ObjectInputStream(stream)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RSH-JV-002", findings[0].RuleId);
    }

    // RSH-JV-003 (JavaWeakCryptoRule)

    [Fact]
    public void JavaWeakCryptoRule_Triggers_OnMd5()
    {
        var rule = new JavaWeakCryptoRule();
        var ctx = Jv("MessageDigest.getInstance(\"MD5\")");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RSH-JV-003", findings[0].RuleId);
    }

    // RSH-JV-004 (JavaPathTraversalRule)

    [Fact]
    public void JavaPathTraversalRule_Triggers_OnDynamicFile()
    {
        var rule = new JavaPathTraversalRule();
        var ctx = Jv("new File(basePath + \"/\" + userFile)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RSH-JV-004", findings[0].RuleId);
    }

    // RSH-JV-005 (JavaXmlXxeRule)

    [Fact]
    public void JavaXmlXxeRule_Triggers_OnUnsecuredFactory()
    {
        var rule = new JavaXmlXxeRule();
        var ctx = Jv("DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RSH-JV-005", findings[0].RuleId);
    }

    [Fact]
    public void JavaXmlXxeRule_DoesNotTrigger_OnSecuredFactory()
    {
        var rule = new JavaXmlXxeRule();
        var ctx = Jv("DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();\nfactory.setFeature(\"http://apache.org/xml/features/disallow-doctype-decl\", true);");

        Assert.Empty(rule.Analyze(ctx));
    }
}
