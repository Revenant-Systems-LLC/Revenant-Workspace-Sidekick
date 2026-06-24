using RevenantWorkspaceScout.Core.Models;
using RevenantWorkspaceScout.Rules.Binary;
using Xunit;

namespace RevenantWorkspaceScout.Tests.Rules;

public class BinaryRulesTests
{
    // RWS-BIN-001

    [Fact]
    public void BinarySecretRule_Triggers_OnEmbeddedOpenAiKey()
    {
        var rule = new BinarySecretRule();
        var ctx = Dll("some binary strings\nsk-abcdefghijklmnopqrstuvwxyz1234567890ab\nmore strings");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-BIN-001", f.RuleId));
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void BinarySecretRule_Triggers_OnEmbeddedAwsKey()
    {
        var rule = new BinarySecretRule();
        var ctx = Dll("AKIAIOSFODNN7EXAMPLE");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-BIN-001", f.RuleId));
    }

    [Fact]
    public void BinarySecretRule_DoesNotTrigger_OnCleanBinary()
    {
        var rule = new BinarySecretRule();
        var ctx = Dll("This program cannot be run in DOS mode\nSomeAssembly.dll\nSystem.String");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-BIN-002

    [Fact]
    public void BinaryConnectionStringRule_Triggers_OnEmbeddedConnStr()
    {
        var rule = new BinaryConnectionStringRule();
        var ctx = Dll("Server=prod-db;Initial Catalog=mydb;User Id=sa;Password=S3cr3t!");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-BIN-002", f.RuleId));
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void BinaryConnectionStringRule_Triggers_OnMongoUri()
    {
        var rule = new BinaryConnectionStringRule();
        var ctx = Dll("mongodb://admin:hunter2secret@mongo.internal:27017");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-BIN-002", f.RuleId));
    }

    [Fact]
    public void BinaryConnectionStringRule_DoesNotTrigger_OnIntegratedSecurity()
    {
        var rule = new BinaryConnectionStringRule();
        var ctx = Dll("Server=mydb;Database=myapp;Integrated Security=true");

        Assert.Empty(rule.Analyze(ctx));
    }

    private static FileContext Dll(string extractedStrings) =>
        new("MyApp.dll", "MyApp.dll", extractedStrings);
}
