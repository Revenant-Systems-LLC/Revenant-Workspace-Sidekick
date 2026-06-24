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
    bool StudentMode = false
)
{
    public static ScanOptions Default(string path) =>
        new(path, "console", null, false, false, Severity.Low, [], [], false);
}
