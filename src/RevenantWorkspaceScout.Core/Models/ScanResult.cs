namespace RevenantWorkspaceScout.Core.Models;

public sealed record ScanResult(
    string ScanRoot,
    IReadOnlyList<Finding> Findings,
    int Score,
    char Grade,
    int FilesScanned,
    TimeSpan Duration,
    int SuppressedCount = 0
);
