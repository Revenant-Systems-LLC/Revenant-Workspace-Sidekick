using RevenantWorkspaceScout.Core.Models;
using RevenantWorkspaceScout.Rules.Python;
using Xunit;

namespace RevenantWorkspaceScout.Tests.Rules;

public class PythonRulesTests
{
    private static FileContext Py(string code) =>
        new("app.py", "app.py", code);

    // RWS-PY-001 (DangerousEvalRule)

    [Fact]
    public void DangerousEvalRule_Triggers_OnDynamicEval()
    {
        var rule = new DangerousEvalRule();
        var ctx = Py("eval(user_input)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-001", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void DangerousEvalRule_Triggers_OnInterpolatedExec()
    {
        var rule = new DangerousEvalRule();
        var ctx = Py("exec(f'print({var})')");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-001", findings[0].RuleId);
    }

    [Fact]
    public void DangerousEvalRule_DoesNotTrigger_OnLiteralEval()
    {
        var rule = new DangerousEvalRule();
        var ctx = Py("eval('1 + 1')");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PY-002 (SubprocessShellRule)

    [Fact]
    public void SubprocessShellRule_Triggers_OnShellTrue()
    {
        var rule = new SubprocessShellRule();
        var ctx = Py("subprocess.run('ls', shell=True)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-002", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void SubprocessShellRule_Triggers_OnOsSystem()
    {
        var rule = new SubprocessShellRule();
        var ctx = Py("os.system('rm -rf /')");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-002", findings[0].RuleId);
    }

    [Fact]
    public void SubprocessShellRule_DoesNotTrigger_OnShellFalse()
    {
        var rule = new SubprocessShellRule();
        var ctx = Py("subprocess.run(['ls', '-la'], shell=False)");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PY-003 (InsecureDeserializationRule)

    [Fact]
    public void InsecureDeserializationRule_Triggers_OnPickleLoads()
    {
        var rule = new InsecureDeserializationRule();
        var ctx = Py("pickle.loads(payload)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-003", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void InsecureDeserializationRule_Triggers_OnYamlUnsafeLoad()
    {
        var rule = new InsecureDeserializationRule();
        var ctx = Py("yaml.unsafe_load(data)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-003", findings[0].RuleId);
    }

    [Fact]
    public void InsecureDeserializationRule_DoesNotTrigger_OnJsonLoads()
    {
        var rule = new InsecureDeserializationRule();
        var ctx = Py("json.loads(payload)");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PY-004 (WeakCryptographyRule)

    [Fact]
    public void WeakCryptographyRule_Triggers_OnMd5()
    {
        var rule = new WeakCryptographyRule();
        var ctx = Py("hashlib.md5(b'hello')");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-004", findings[0].RuleId);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void WeakCryptographyRule_DoesNotTrigger_OnSha256()
    {
        var rule = new WeakCryptographyRule();
        var ctx = Py("hashlib.sha256(b'hello')");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-PY-005 (InsecureWebConfigRule)

    [Fact]
    public void InsecureWebConfigRule_Triggers_OnFlaskDebugTrue()
    {
        var rule = new InsecureWebConfigRule();
        var ctx = Py("app.run(debug=True)");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-005", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void InsecureWebConfigRule_Triggers_OnHardcodedKey()
    {
        var rule = new InsecureWebConfigRule();
        var ctx = Py("SECRET_KEY = 'super-secret-random-key'");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-005", findings[0].RuleId);
    }

    [Fact]
    public void InsecureWebConfigRule_Triggers_OnCorsWildcard()
    {
        var rule = new InsecureWebConfigRule();
        var ctx = Py("CORS(app, origins='*')");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-PY-005", findings[0].RuleId);
    }

    [Fact]
    public void InsecureWebConfigRule_DoesNotTrigger_OnDebugFalse()
    {
        var rule = new InsecureWebConfigRule();
        var ctx = Py("app.run(debug=False)");

        Assert.Empty(rule.Analyze(ctx));
    }
}
