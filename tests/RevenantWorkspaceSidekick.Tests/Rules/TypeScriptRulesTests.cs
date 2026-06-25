using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Rules.TypeScript;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests.Rules;

public class TypeScriptRulesTests
{
    private static FileContext Ts(string code) =>
        new("app.ts", "app.ts", code);

    // RWS-TS-001 (TsDangerousEvalRule)

    [Fact]
    public void TsDangerousEvalRule_Triggers_OnEval()
    {
        var rule = new TsDangerousEvalRule();
        var ctx = Ts("eval(input)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-001", findings[0].RuleId);
    }

    [Fact]
    public void TsDangerousEvalRule_Triggers_OnTimerString()
    {
        var rule = new TsDangerousEvalRule();
        var ctx = Ts("setTimeout('doSomething()', 1000)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-001", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    // RWS-TS-002 (TsCommandExecutionRule)

    [Fact]
    public void TsCommandExecutionRule_Triggers_OnDynamicExec()
    {
        var rule = new TsCommandExecutionRule();
        var ctx = Ts("exec(`ping ${host}`)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-002", findings[0].RuleId);
    }

    // RWS-TS-003 (TsInsecureCryptoRule)

    [Fact]
    public void TsInsecureCryptoRule_Triggers_OnMd5()
    {
        var rule = new TsInsecureCryptoRule();
        var ctx = Ts("crypto.createHash('md5')");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-003", findings[0].RuleId);
    }

    // RWS-TS-004 (TsPrototypePollutionRule)

    [Fact]
    public void TsPrototypePollutionRule_Triggers_OnProtoRef()
    {
        var rule = new TsPrototypePollutionRule();
        var ctx = Ts("obj['__proto__'] = vuln");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-004", findings[0].RuleId);
    }

    // RWS-TS-005 (TsReactHtmlInjectionRule)

    [Fact]
    public void TsReactHtmlInjectionRule_Triggers_OnDangerouslySetHtml()
    {
        var rule = new TsReactHtmlInjectionRule();
        var ctx = Ts("<div dangerouslySetInnerHTML={{__html: htmlVal}} />");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-TS-005", findings[0].RuleId);
    }
}
