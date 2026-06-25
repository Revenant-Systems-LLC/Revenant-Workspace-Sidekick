using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Rules;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests.Integration;

public class ScanIntegrationTests
{
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "CursedApp");

    [Fact]
    public void Scan_CursedApp_ReturnsFindings()
    {
        Assert.True(Directory.Exists(FixturePath), $"Fixture missing: {FixturePath}");

        var options = ScanOptions.Default(FixturePath);
        var result = RuleEngine.Scan(RuleRegistry.All, options);

        Assert.True(result.FilesScanned > 0, "No files were scanned");
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void Scan_CursedApp_GetsGradeF()
    {
        var options = ScanOptions.Default(FixturePath);
        var result = RuleEngine.Scan(RuleRegistry.All, options);

        Assert.Equal('F', result.Grade);
    }

    [Theory]
    [InlineData("RWS-MSIX-001")]
    [InlineData("RWS-MSIX-002")]
    [InlineData("RWS-MSIX-003")]
    [InlineData("RWS-REG-001")]
    [InlineData("RWS-REG-002")]
    [InlineData("RWS-REG-003")]
    [InlineData("RWS-EXEC-001")]
    [InlineData("RWS-EXEC-002")]
    [InlineData("RWS-EXEC-003")]
    [InlineData("RWS-EXEC-004")]
    [InlineData("RWS-EXEC-005")]
    [InlineData("RWS-REG-004")]
    [InlineData("RWS-SEC-001")]
    [InlineData("RWS-SEC-002")]
    [InlineData("RWS-SEC-003")]
    [InlineData("RWS-SEC-004")]
    [InlineData("RWS-MSIX-004")]
    [InlineData("RWS-XAML-001")]
    [InlineData("RWS-XAML-002")]
    [InlineData("RWS-XAML-003")]
    [InlineData("RWS-PINVOKE-001")]
    [InlineData("RWS-PINVOKE-002")]
    [InlineData("RWS-PINVOKE-003")]
    [InlineData("RWS-ACL-001")]
    [InlineData("RWS-ACL-002")]
    public void Scan_CursedApp_FindsExpectedRuleId(string expectedRuleId)
    {
        var options = ScanOptions.Default(FixturePath);
        var result = RuleEngine.Scan(RuleRegistry.All, options);

        var ruleIds = result.Findings.Select(f => f.RuleId).ToHashSet();
        Assert.Contains(expectedRuleId, ruleIds);
    }

    [Fact]
    public void Scan_EmptyDirectory_ReturnsCleanResult()
    {
        var empty = Path.Combine(Path.GetTempPath(), $"RWS-empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(empty);
        try
        {
            var options = ScanOptions.Default(empty);
            var result = RuleEngine.Scan(RuleRegistry.All, options);

            Assert.Empty(result.Findings);
            Assert.Equal(100, result.Score);
            Assert.Equal('A', result.Grade);
        }
        finally
        {
            Directory.Delete(empty, recursive: true);
        }
    }
}
