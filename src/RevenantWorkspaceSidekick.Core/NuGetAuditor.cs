using System.Diagnostics;
using System.Text.RegularExpressions;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

/// <summary>
/// Shells out to `dotnet list package --vulnerable --include-transitive` and converts
/// the output into RWS-DEP-* findings.  Requires .NET SDK on PATH.
/// Skipped gracefully if dotnet is not available or there are no *.csproj files.
/// </summary>
public static partial class NuGetAuditor
{
    // Matches lines like:
    //   > Newtonsoft.Json   12.0.3   12.0.3   High   https://github.com/advisories/GHSA-xxx
    [GeneratedRegex(@">\s+(?<pkg>\S+)\s+\S+\s+\S+\s+(?<sev>Critical|High|Moderate|Low)\s+(?<url>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex VulnerablePackageLine();

    public static IReadOnlyList<Finding> Audit(string root)
    {
        var findings = new List<Finding>();

        if (!Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories).Any())
            return findings;

        string output;
        try
        {
            output = Run(root, "list package --vulnerable --include-transitive");
        }
        catch
        {
            // dotnet not on PATH or command failed — skip silently
            return findings;
        }

        string? currentProject = null;

        foreach (var line in output.Split('\n'))
        {
            if (line.TrimStart().StartsWith("Project '"))
            {
                var m = Regex.Match(line, @"Project '(?<name>[^']+)'");
                currentProject = m.Success ? m.Groups["name"].Value : "Unknown";
                continue;
            }

            var match = VulnerablePackageLine().Match(line);
            if (!match.Success) continue;

            var pkg = match.Groups["pkg"].Value;
            var rawSev = match.Groups["sev"].Value;
            var url = match.Groups["url"].Value;
            var severity = MapSeverity(rawSev);

            findings.Add(new Finding(
                RuleId: "RWS-DEP-001",
                Title: $"Vulnerable NuGet package: {pkg}",
                Severity: severity,
                File: currentProject ?? root,
                Line: null,
                Why: $"Package '{pkg}' has a known {rawSev} vulnerability. See: {url}",
                Fix: "Update this package to a patched version. Run: dotnet add package <name> --version <latest>",
                Example: $"dotnet add package {pkg} --version <latest-safe>"
            ));
        }

        return findings;
    }

    private static Severity MapSeverity(string s) => s.ToLowerInvariant() switch
    {
        "critical" => Severity.Critical,
        "high" => Severity.High,
        "moderate" or "medium" => Severity.Medium,
        _ => Severity.Low
    };

    private static string Run(string root, string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0) throw new InvalidOperationException(proc.StandardError.ReadToEnd());
        return stdout;
    }
}
