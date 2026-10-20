using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Rules.Secrets;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests;

public class RuleEngineTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rws-ruleengine-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    // Mirrors SEC-001 on `apiKey = "sk-proj-..."`: the prefix pass and the regex pass
    // both fire on the same line at the same severity. Only the prefix-pass finding
    // carries the RawValue that --verify needs, and its title sorts after the regex
    // pass's, so a Title-only tie-break kept the finding --verify skips.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scan_Dedup_PrefersFindingWithRawValue_OnSeverityTie(bool rawValueFirst)
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "Secrets.cs"), "const string apiKey = \"placeholder\";");
            var raw = "sk-proj-" + new string('A', 40);
            var withRaw = new Finding("RWS-SEC-001", "Hardcoded OpenAI project key detected",
                Severity.Critical, "Secrets.cs", 1, "why", "fix", RawValue: raw);
            var withoutRaw = new Finding("RWS-SEC-001", "Hardcoded API key/secret detected",
                Severity.Critical, "Secrets.cs", 1, "why", "fix");
            Finding[] emitted = rawValueFirst ? [withRaw, withoutRaw] : [withoutRaw, withRaw];

            var result = RuleEngine.Scan([new StubRule(emitted)], ScanOptions.Default(dir));

            var kept = Assert.Single(result.Findings);
            Assert.Equal(raw, kept.RawValue);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Scan_ApiKeyAssignment_KeepsRawValueForVerify()
    {
        var dir = CreateTempDir();
        try
        {
            // sk-proj- + 40 chars = 48, inside the prefix pass's [36,80] length window.
            var key = "sk-proj-" + new string('A', 40);
            File.WriteAllText(Path.Combine(dir, "Secrets.cs"), $"const string apiKey = \"{key}\";");

            var result = RuleEngine.Scan([new ApiKeyPatternRule()], ScanOptions.Default(dir));

            var kept = Assert.Single(result.Findings);
            Assert.Equal(key, kept.RawValue);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubRule(Finding[] findings) : IRule
    {
        public RuleMetadata Metadata { get; } = new("RWS-SEC-001", "Stub", Severity.Critical, [".cs"]);

        public IEnumerable<Finding> Analyze(FileContext context) => findings;
    }
}
