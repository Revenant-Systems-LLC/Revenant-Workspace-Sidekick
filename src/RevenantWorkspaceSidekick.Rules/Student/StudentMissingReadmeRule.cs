using System.IO;
using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Rules.Student;

/// <summary>RWS-EDU-CD-01: Checks for missing README.md at the root.</summary>
public sealed class StudentMissingReadmeRule : IRule
{
    // This rule is a bit different. We only want to alert ONCE per project.
    // rws processes file-by-file, so we can check if the current file is in the root directory,
    // and if the root directory is missing a README.md. We use a static flag to only report once.
    private static readonly object _lock = new();
    private static string? _lastScannedRoot = null;
    private static bool _alreadyReported = false;

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-EDU-CD-01",
        Title: "Missing README.md in Project Root",
        DefaultSeverity: Severity.Info,
        FileExtensions: [".csproj", ".py", ".js", ".ts", ".sln"] // Trigger on common project files
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        lock (_lock)
        {
            var rootPath = context.Path.Substring(0, context.Path.Length - context.RelativePath.Length);

            // Reset if we are in a new scan root
            if (_lastScannedRoot != rootPath)
            {
                _lastScannedRoot = rootPath;
                _alreadyReported = false;
            }

            if (_alreadyReported)
                yield break;

            var readmePath = Path.Combine(rootPath, "README.md");

            if (!File.Exists(readmePath) && !File.Exists(Path.Combine(rootPath, "readme.md")))
            {
                _alreadyReported = true;
                yield return new Finding(
                    RuleId: Metadata.Id,
                    Title: Metadata.Title,
                    Severity: Metadata.DefaultSeverity,
                    File: "README.md",
                    Line: null,
                    Why: "A professional portfolio piece requires documentation. Without a README, reviewers (and hiring managers) won't know how to run or evaluate your code.",
                    Fix: "Create a README.md in the root directory detailing what the project does, how to set it up, and how to run it."
                );
            }
        }
    }
}
