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
        // Get unified diff of the last N commits, then extract added hunks only.
        // We only care about lines that were added (+ lines); those could be secrets.
        var log = Run(root, $"log -p --no-merges -n {depth} --diff-filter=A --unified=0");
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
        try { Run(root, "rev-parse --git-dir"); return true; }
        catch { return false; }
    }

    private static string Run(string root, string arguments)
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
        return stdout;
    }
}
