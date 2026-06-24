using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;
using Xunit;

namespace RevenantWorkspaceScout.Tests;

public class SuppressionCheckerTests
{
    private static Finding MakeFinding(string ruleId, int? line) => new(
        RuleId: ruleId,
        Title: "test",
        Severity: Severity.High,
        File: "test.cs",
        Line: line,
        Why: "why",
        Fix: "fix");

    // ── Same-line suppression ──────────────────────────────────────────────

    [Fact]
    public void IsSuppressed_True_WhenAnnotationOnSameLine()
    {
        var content = "Process.Start(psi); // RWS-suppress: RWS-EXEC-001";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 1)));
    }

    [Fact]
    public void IsSuppressed_True_WhenAnnotationOnLineAbove()
    {
        var content = "// RWS-suppress: RWS-EXEC-001\nProcess.Start(psi);";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 2)));
    }

    // ── Works across comment styles ────────────────────────────────────────

    [Fact]
    public void IsSuppressed_True_XmlStyleComment()
    {
        var content = "<!-- RWS-suppress: RWS-MSIX-002 -->\n<rescap:Capability Name=\"runFullTrust\" />";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-MSIX-002", 2)));
    }

    [Fact]
    public void IsSuppressed_True_CaseInsensitiveRuleId()
    {
        var content = "// RWS-suppress: RWS-exec-001\nProcess.Start(psi);";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 2)));
    }

    [Fact]
    public void IsSuppressed_True_WithTrailingReason()
    {
        var content = "// RWS-suppress: RWS-EXEC-001 UAC elevation helper — internal path, no user input\nProcess.Start(psi);";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 2)));
    }

    // ── Negative cases ─────────────────────────────────────────────────────

    [Fact]
    public void IsSuppressed_False_WhenDifferentRuleId()
    {
        var content = "// RWS-suppress: RWS-EXEC-002\nProcess.Start(psi);";
        Assert.False(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 2)));
    }

    [Fact]
    public void IsSuppressed_False_WhenAnnotationTwoLinesAbove()
    {
        var content = "// RWS-suppress: RWS-EXEC-001\n\nProcess.Start(psi);";
        Assert.False(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 3)));
    }

    [Fact]
    public void IsSuppressed_False_WhenNoAnnotation()
    {
        var content = "Process.Start(psi);";
        Assert.False(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 1)));
    }

    [Fact]
    public void IsSuppressed_True_WhenLineIsNull_AndAnnotationAnywhereInFile()
    {
        // Rules that don't emit a line number (e.g. MSIX manifest rules) are suppressed
        // by a file-level annotation anywhere in the file.
        var content = "<!-- RWS-suppress: RWS-MSIX-002 required for UAC helper -->\n<rescap:Capability Name=\"runFullTrust\" />";
        Assert.True(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-MSIX-002", null)));
    }

    [Fact]
    public void IsSuppressed_False_WhenLineIsNull_AndNoAnnotationInFile()
    {
        var content = "<rescap:Capability Name=\"runFullTrust\" />";
        Assert.False(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-MSIX-002", null)));
    }

    [Fact]
    public void IsSuppressed_False_WhenLineOutOfRange()
    {
        var content = "Process.Start(psi);";
        Assert.False(SuppressionChecker.IsSuppressed(content, MakeFinding("RWS-EXEC-001", 999)));
    }
}
