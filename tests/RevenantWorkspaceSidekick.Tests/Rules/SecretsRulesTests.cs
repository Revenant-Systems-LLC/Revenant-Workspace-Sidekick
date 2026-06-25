using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Rules.Secrets;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests.Rules;

public class SecretsRulesTests
{
    // RWS-SEC-001

    [Fact]
    public void ApiKeyPatternRule_Triggers_OnOpenAiKey()
    {
        var rule = new ApiKeyPatternRule();
        var ctx = Json("""{ "apiKey": "sk-abcdefghijklmnopqrstuvwxyz12345678901234" }""");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-001", f.RuleId));
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void ApiKeyPatternRule_Triggers_OnGithubPat()
    {
        var rule = new ApiKeyPatternRule();
        var ctx = Json("""{ "token": "ghp_AAAABBBBCCCCDDDDEEEEFFFFGGGG12345678" }""");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-001", f.RuleId));
    }

    [Fact]
    public void ApiKeyPatternRule_Triggers_OnAwsKey()
    {
        var rule = new ApiKeyPatternRule();
        var ctx = Json("""{ "key": "AKIAIOSFODNN7EXAMPLE" }""");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-001", f.RuleId));
    }

    [Fact]
    public void ApiKeyPatternRule_DoesNotTrigger_OnShortValues()
    {
        var rule = new ApiKeyPatternRule();
        var ctx = Json("""{ "name": "CursedApp" }""");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-SEC-002

    [Fact]
    public void ResourceSecretRule_Triggers_OnConnectionStringWithPassword()
    {
        var rule = new ResourceSecretRule();
        var ctx = ConfigJson(
            """{ "ConnectionStrings": { "Default": "Server=db;Password=SuperSecret123" } }""");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-002", f.RuleId));
    }

    [Fact]
    public void ResourceSecretRule_Triggers_OnPasswordField()
    {
        var rule = new ResourceSecretRule();
        var ctx = ConfigJson("""{ "Database": { "Password": "hunter2secret" } }""");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-002", f.RuleId));
    }

    [Fact]
    public void ResourceSecretRule_DoesNotTrigger_OnInnocuousJson()
    {
        var rule = new ResourceSecretRule();
        var ctx = ConfigJson("""{ "AppName": "MyApp", "Version": "1.0" }""");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-SEC-003

    [Fact]
    public void ProjectMetadataSecretRule_Triggers_OnGithubPatInCsproj()
    {
        var rule = new ProjectMetadataSecretRule();
        var ctx = Csproj("""
            <PropertyGroup>
              <DeployToken>ghp_AAAABBBBCCCCDDDDEEEEFFFFGGGG12345678</DeployToken>
            </PropertyGroup>
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-003", f.RuleId));
    }

    [Fact]
    public void ProjectMetadataSecretRule_DoesNotTrigger_OnNormalProperties()
    {
        var rule = new ProjectMetadataSecretRule();
        var ctx = Csproj("""
            <PropertyGroup>
              <Version>1.0.0</Version>
              <Authors>David Fisher</Authors>
            </PropertyGroup>
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-SEC-004

    [Fact]
    public void ConnectionStringInCodeRule_Triggers_OnAdoNetConnStr()
    {
        var rule = new ConnectionStringInCodeRule();
        var ctx = Cs("string conn = \"Server=prod;Initial Catalog=mydb;User Id=sa;Password=S3cr3t!\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-SEC-004", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void ConnectionStringInCodeRule_Triggers_OnMongoUri()
    {
        var rule = new ConnectionStringInCodeRule();
        var ctx = Cs("var uri = \"mongodb://admin:hunter2secret@mongo.internal:27017\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-SEC-004", findings[0].RuleId);
    }

    [Fact]
    public void ConnectionStringInCodeRule_Triggers_OnPostgresUri()
    {
        var rule = new ConnectionStringInCodeRule();
        var ctx = Cs("var uri = \"postgres://user:hunter2@db.internal/mydb\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-SEC-004", findings[0].RuleId);
    }

    [Fact]
    public void ConnectionStringInCodeRule_DoesNotTrigger_OnConnStrWithoutPassword()
    {
        var rule = new ConnectionStringInCodeRule();
        var ctx = Cs("string conn = \"Server=mydb;Database=myapp;Integrated Security=true;\";");

        Assert.Empty(rule.Analyze(ctx));
    }

    [Fact]
    public void ConnectionStringInCodeRule_DoesNotTrigger_OnConfigReference()
    {
        var rule = new ConnectionStringInCodeRule();
        var ctx = Cs("var conn = config.GetConnectionString(\"Default\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RedactedSnippet (from your-welcome blinding engine)

    [Fact]
    public void ApiKeyPatternRule_PopulatesRedactedSnippet_NotPlaintext()
    {
        var rule = new ApiKeyPatternRule();
        var secret = "AKIAIOSFODNN7EXAMPLE";
        var ctx = Json($"{{\"key\": \"{secret}\"}}");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        var snippet = findings[0].RedactedSnippet;
        Assert.NotNull(snippet);
        Assert.DoesNotContain(secret, snippet);   // plaintext must not appear in report
        Assert.Contains("****", snippet);
    }

    [Fact]
    public void ApiKeyPatternRule_Triggers_OnSlackBotToken()
    {
        var rule = new ApiKeyPatternRule();
        // xoxb- + 45 alphanumeric chars = 50 chars total
        var token = "xoxb-" + new string('A', 45);
        var ctx = Cs($"var t = \"{token}\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-001", f.RuleId));
    }

    [Fact]
    public void ApiKeyPatternRule_Triggers_OnGoogleApiKey()
    {
        var rule = new ApiKeyPatternRule();
        // AIza + 35 chars = 39 total, within [35,45]
        var key = "AIza" + new string('B', 35);
        var ctx = Cs($"var k = \"{key}\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal("RWS-SEC-001", f.RuleId));
    }

    [Fact]
    public void SecretBlinder_Blinds_MiddleOfSecret()
    {
        Assert.Equal("AKIA****MPLE", SecretBlinder.Blind("AKIAIOSFODNN7EXAMPLE"));
        Assert.Equal("****", SecretBlinder.Blind("abcd"));          // short (≤8) → all stars
        Assert.Equal("********", SecretBlinder.Blind("abcdefgh")); // exactly 8 → all stars
        Assert.Equal("abcd****ijkl", SecretBlinder.Blind("abcdefghijkl")); // >8 → first4+****+last4
    }

    private static FileContext Json(string content) =>
        new("config.json", "config.json", content);

    private static FileContext ConfigJson(string content) =>
        new("appsettings.json", "appsettings.json", content);

    private static FileContext Csproj(string propsXml) =>
        new("App.csproj", "App.csproj",
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{propsXml}</Project>");

    private static FileContext Cs(string code) =>
        new("App.cs", "App.cs", code);

    // --- Entropy detection (Feature: Shannon entropy pass) ---

    [Fact]
    public void EntropyScorer_HighEntropy_Random32Chars()
    {
        // A realistic random Base64-like string should have entropy > 4.5
        var entropy = EntropyScorer.Shannon("aB3kQzP9mXvNrTyWdLsUeHfJ2gCiOo7E");
        Assert.True(entropy >= 4.0, $"Expected entropy ≥ 4.0, got {entropy:F2}");
    }

    [Fact]
    public void EntropyScorer_LowEntropy_RepeatedChars()
    {
        var entropy = EntropyScorer.Shannon("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        Assert.True(entropy < 1.0, $"Expected entropy < 1.0, got {entropy:F2}");
    }

    [Fact]
    public void ApiKeyPatternRule_EntropyPass_FlagsHighEntropyString()
    {
        var rule = new ApiKeyPatternRule();
        // 32-char high-entropy string in a quoted literal — no known prefix
        var highEntropyToken = "aB3kQzP9mXvNrTyWdLsUeHfJ2gCiOo7E";
        var ctx = Cs($"var token = \"{highEntropyToken}\";");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Contains(findings, f => f.Title.Contains("High-entropy") && f.RuleId == "RWS-SEC-001");
    }

    [Fact]
    public void ApiKeyPatternRule_EntropyPass_DoesNotFlagLowEntropyString()
    {
        var rule = new ApiKeyPatternRule();
        var lowEntropyToken = "helloWorldhelloWorldhelloWorldhello";
        var ctx = Cs($"var msg = \"{lowEntropyToken}\";");

        var findings = rule.Analyze(ctx)
            .Where(f => f.Title.Contains("High-entropy"))
            .ToList();
        Assert.Empty(findings);
    }

    // --- Taint tracking (Feature: RWS-EXEC-006) ---

    [Fact]
    public void TaintedProcessStartRule_Triggers_WhenArgsFlowsToProcessStart()
    {
        var rule = new RevenantWorkspaceSidekick.Rules.Execution.TaintedProcessStartRule();
        var ctx = Cs("""
            var exe = args[0];
            Process.Start(exe);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.NotEmpty(findings);
        Assert.Equal("RWS-EXEC-006", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void TaintedProcessStartRule_DoesNotTrigger_WhenNoTaintSource()
    {
        var rule = new RevenantWorkspaceSidekick.Rules.Execution.TaintedProcessStartRule();
        var ctx = Cs("""
            var exe = "notepad.exe";
            Process.Start(exe);
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Empty(findings);
    }

    [Fact]
    public void TaintedProcessStartRule_Triggers_OnEnvVarToProcessStart()
    {
        var rule = new RevenantWorkspaceSidekick.Rules.Execution.TaintedProcessStartRule();
        var ctx = Cs("""
            var tool = Environment.GetEnvironmentVariable("MY_TOOL");
            Process.Start(tool, "--help");
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Contains(findings, f => f.RuleId == "RWS-EXEC-006");
    }

    // --- Baseline manager (Feature: baseline suppression) ---

    [Fact]
    public void BaselineManager_FilterNew_SuppressesKnownFindings()
    {
        var known = new Finding("RWS-SEC-001", "Title", Severity.High, "file.cs", 42, "why", "fix");
        var newFinding = new Finding("RWS-SEC-001", "Other", Severity.High, "file.cs", 99, "why", "fix");
        var baseline = new RevenantWorkspaceSidekick.Core.BaselineFile(
            "2026-01-01", "1",
            [new("RWS-SEC-001", "file.cs", 42, "Title")]);

        var result = BaselineManager.FilterNew([known, newFinding], baseline);
        Assert.Single(result);
        Assert.Equal(99, result[0].Line);
    }

    [Fact]
    public void BaselineManager_FilterNew_NoBaseline_ReturnsAll()
    {
        var findings = new[]
        {
            new Finding("RWS-SEC-001", "T", Severity.High, "f.cs", 1, "w", "fix"),
            new Finding("RWS-SEC-002", "T", Severity.Medium, "f.cs", 2, "w", "fix"),
        };
        var result = BaselineManager.FilterNew(findings, null);
        Assert.Equal(2, result.Count);
    }

    // --- SARIF output (Feature: --format sarif) ---

    [Fact]
    public void SarifReporter_Emits_ValidSarifStructure()
    {
        var reporter = new RevenantWorkspaceSidekick.Core.Reporters.SarifReporter();
        var findings = new[]
        {
            new Finding("RWS-SEC-001", "Hardcoded key", Severity.Critical, "src/foo.cs", 42,
                "Why it matters", "Fix it"),
        };
        var result = new RevenantWorkspaceSidekick.Core.Models.ScanResult(
            "C:\\MyApp", findings, 75, 'C', 10, TimeSpan.FromSeconds(1));

        using var sw = new StringWriter();
        reporter.Report(result, sw);
        var json = sw.ToString();

        Assert.Contains("\"version\": \"2.1.0\"", json);
        Assert.Contains("\"RWS-SEC-001\"", json);
        Assert.Contains("\"startLine\": 42", json);
        Assert.Contains("%SRCROOT%", json);
    }
}
