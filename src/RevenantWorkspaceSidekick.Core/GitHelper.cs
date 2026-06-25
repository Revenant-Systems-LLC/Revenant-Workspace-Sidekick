using System.Diagnostics;

namespace RevenantWorkspaceSidekick.Core;

public static class GitHelper
{
    /// <summary>Returns paths of files changed relative to HEAD (unstaged + staged + untracked).</summary>
    public static IReadOnlyList<string> GetChangedFiles(string root)
    {
        // staged + unstaged
        var staged = Run(root, "diff --name-only HEAD");
        // untracked
        var untracked = Run(root, "ls-files --others --exclude-standard");
        return staged.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Concat(untracked.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(p => p.Trim().Replace('/', Path.DirectorySeparatorChar))
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Yields (relativePath, content) pairs for file blobs from historical commits
    /// that no longer exist at HEAD — the classic "deleted secret" pattern.
    /// </summary>
    public static IEnumerable<(string RelativePath, string Content)> GetHistoricalBlobs(string root, int depth = 100)
    {
        // --diff-filter=AM: include Added files AND Modified files so secrets added
        // to an existing file and later removed are not missed (the common real-world case).
        var log = Run(root, $"log -p --no-merges -n {depth} --diff-filter=AM --unified=0");
        if (string.IsNullOrWhiteSpace(log)) yield break;

        string? currentFile = null;
        var sb = new System.Text.StringBuilder();

        foreach (var rawLine in log.Split('\n'))
        {
            if (rawLine.StartsWith("diff --git "))
            {
                if (currentFile is not null && sb.Length > 0)
                {
                    yield return (currentFile, sb.ToString());
                    sb.Clear();
                }
                // Extract b/path
                var parts = rawLine.Split(' ');
                currentFile = parts.Length >= 4
                    ? parts[3].TrimStart('b', '/').Replace('/', Path.DirectorySeparatorChar)
                    : null;
                continue;
            }

            // Collect lines that were added in history
            if (rawLine.StartsWith("+") && !rawLine.StartsWith("+++"))
                sb.AppendLine(rawLine[1..]);
        }

        if (currentFile is not null && sb.Length > 0)
            yield return (currentFile, sb.ToString());
    }

    public static bool IsGitRepo(string root)
    {
        try { return RunWithExitCode(root, "rev-parse --git-dir").ExitCode == 0; }
        catch { return false; }
    }

    /// <summary>Returns the absolute path of the repo root, or null if not in a git repo.</summary>
    public static string? GetRepoRoot(string root)
    {
        try
        {
            var (stdout, exitCode) = RunWithExitCode(root, "rev-parse --show-toplevel");
            return exitCode == 0 ? stdout.Trim() : null;
        }
        catch { return null; }
    }

    private static string Run(string root, string arguments) =>
        RunWithExitCode(root, arguments).Stdout;

    private static (string Stdout, int ExitCode) RunWithExitCode(string root, string arguments)
    {
        var psi = new ProcessStartInfo("git", arguments)
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
        return (stdout, proc.ExitCode);
    }
}
