namespace RevenantWorkspaceSidekick.Core.Models;

/// <summary>
/// Plain file context passed to rules. Rules are responsible for any parsing they need.
/// Core has no Roslyn dependency.
/// </summary>
public sealed record FileContext(
    string Path,
    string RelativePath,
    string Content
);
