using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests;

public class ScorerTests
{
    [Fact]
    public void Score_Is100_WhenNoFindings()
    {
        var (score, grade) = Scorer.Calculate([]);
        Assert.Equal(100, score);
        Assert.Equal('A', grade);
    }

    [Theory]
    [InlineData(100, 'A')]
    [InlineData(90,  'A')]
    [InlineData(89,  'B')]
    [InlineData(80,  'B')]
    [InlineData(79,  'C')]
    [InlineData(70,  'C')]
    [InlineData(69,  'D')]
    [InlineData(60,  'D')]
    [InlineData(59,  'F')]
    [InlineData(0,   'F')]
    public void GradeBoundaries_AreCorrect(int score, char expectedGrade)
    {
        var grade = score switch
        {
            >= 90 => 'A',
            >= 80 => 'B',
            >= 70 => 'C',
            >= 60 => 'D',
            _ => 'F'
        };
        Assert.Equal(expectedGrade, grade);
    }

    [Fact]
    public void OneCritical_Subtracts25()
    {
        var findings = new[] { MakeFinding(Severity.Critical) };
        var (score, _) = Scorer.Calculate(findings);
        Assert.Equal(75, score);
    }

    [Fact]
    public void OneHigh_Subtracts15()
    {
        var findings = new[] { MakeFinding(Severity.High) };
        var (score, _) = Scorer.Calculate(findings);
        Assert.Equal(85, score);
    }

    [Fact]
    public void OneMedium_Subtracts8()
    {
        var findings = new[] { MakeFinding(Severity.Medium) };
        var (score, _) = Scorer.Calculate(findings);
        Assert.Equal(92, score);
    }

    [Fact]
    public void OneLow_Subtracts3()
    {
        var findings = new[] { MakeFinding(Severity.Low) };
        var (score, _) = Scorer.Calculate(findings);
        Assert.Equal(97, score);
    }

    [Fact]
    public void ScoreClampsAtZero_WithManyCriticals()
    {
        var findings = Enumerable.Range(0, 10)
            .Select(_ => MakeFinding(Severity.Critical))
            .ToArray();
        var (score, grade) = Scorer.Calculate(findings);
        Assert.Equal(0, score);
        Assert.Equal('F', grade);
    }

    [Fact]
    public void MixedFindings_ProducesCorrectScore()
    {
        var findings = new[]
        {
            MakeFinding(Severity.Critical), // -25
            MakeFinding(Severity.High),     // -15
            MakeFinding(Severity.Medium),   // -8
            MakeFinding(Severity.Low),      // -3
        };
        var (score, _) = Scorer.Calculate(findings);
        Assert.Equal(100 - 25 - 15 - 8 - 3, score); // 49
    }

    private static Finding MakeFinding(Severity sev) =>
        new("RWS-TEST-001", "Test", sev, "test.cs", null, "why", "fix");
}
