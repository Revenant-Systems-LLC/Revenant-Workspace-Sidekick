using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

public sealed record ScanOptions(
    string ScanPath,
    string Format,
    string? OutputFile,
    bool Offline,
    bool Roast,
    Severity MinSeverity,
    string[] Includes,
    string[] Excludes,
    bool StudentMode = false,
    bool DiffOnly = false,           // --diff: scan only git-changed files
    bool ScanHistory = false,        // --history: scan git log blobs too
    int HistoryDepth = 100,          // --history-depth N
    bool AuditDependencies = false,  // --audit-deps: run NuGet CVE audit
    bool Verify = false,             // --verify: make live HTTP calls to confirm secrets
    bool UpdateBaseline = false,     // --update-baseline: write new .rws-baseline.json
    bool UseBaseline = false         // --baseline: suppress findings already in baseline
)
{
    public static ScanOptions Default(string path) =>
        new(path, "console", null, false, false, Severity.Low, [], [], false);
}
