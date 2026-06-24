using System.Collections.Concurrent;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

public static class RuleEngine
{
    public static ScanResult Scan(IReadOnlyList<IRule> rules, ScanOptions options)
    {
        var root = System.IO.Path.GetFullPath(options.ScanPath);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allFindings = new ConcurrentBag<Finding>();
        var filesScanned = 0;
        var suppressedCount = 0;

        // --- Determine which files to scan ---
        IEnumerable<FileContext> files;
        if (options.DiffOnly && GitHelper.IsGitRepo(root))
        {
            var changedPaths = GitHelper.GetChangedFiles(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
            files = FileWalker.Enumerate(root, options.Includes, options.Excludes)
                .Where(f => changedPaths.Contains(f.RelativePath) || changedPaths.Contains(f.Path));
        }
        else
        {
            files = FileWalker.Enumerate(root, options.Includes, options.Excludes);
        }

        var fileList = files.ToList();

        Parallel.ForEach(fileList, file =>
        {
            Interlocked.Increment(ref filesScanned);

            var ext = System.IO.Path.GetExtension(file.Path);
            foreach (var rule in rules)
            {
                if (!rule.Metadata.FileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    continue;

                try
                {
                    foreach (var finding in rule.Analyze(file))
                    {
                        if (SuppressionChecker.IsSuppressed(file.Content, finding))
                            Interlocked.Increment(ref suppressedCount);
                        else
                            allFindings.Add(finding);
                    }
                }
                catch (Exception ex)
                {
                    allFindings.Add(new Finding(
                        RuleId: "RWS-INTERNAL",
                        Title: $"Rule {rule.Metadata.Id} threw an exception",
                        Severity: Severity.Low,
                        File: file.RelativePath,
                        Line: null,
                        Why: ex.Message,
                        Fix: "This is a scanner bug. Please report it."
                    ));
                }
            }
        });

        // --- Git history scan (--history) ---
        if (options.ScanHistory && GitHelper.IsGitRepo(root))
        {
            foreach (var (relPath, content) in GitHelper.GetHistoricalBlobs(root, options.HistoryDepth))
            {
                var ext = System.IO.Path.GetExtension(relPath);
                var ctx = new FileContext(System.IO.Path.Combine(root, relPath), relPath, content);
                foreach (var rule in rules)
                {
                    if (!rule.Metadata.FileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        continue;
                    // Only run secret rules against history to avoid EXEC/REG noise on deleted code
                    if (!rule.Metadata.Id.StartsWith("RWS-SEC")) continue;
                    try
                    {
                        foreach (var f in rule.Analyze(ctx))
                            allFindings.Add(f with { File = $"[git-history] {relPath}", FromHistory = true });
                    }
                    catch { /* continue */ }
                }
            }
        }

        // --- NuGet CVE audit (--audit-deps) ---
        if (options.AuditDependencies)
        {
            foreach (var f in NuGetAuditor.Audit(root))
                allFindings.Add(f);
        }

        sw.Stop();

        // Dedup: when the same rule fires more than once on the same file+line
        // (e.g. both the AhoCorasick pass and the regex pass in SEC-001 match the
        // same token), keep only the highest-severity instance with a deterministic
        // tie-breaker on RuleId+Title. Different rules are always kept, even on the
        // same line. Null-line findings (XML rules) are never collapsed.
        var filtered = allFindings.Where(f => f.Severity >= options.MinSeverity);
        var deduped = filtered
            .Where(f => f.Line.HasValue)
            .GroupBy(f => (f.File, f.Line!.Value, f.RuleId))
            .Select(g => g.OrderByDescending(f => f.Severity).ThenBy(f => f.Title).First())
            .Concat(filtered.Where(f => !f.Line.HasValue));

        var findings = deduped
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.File)
            .ThenBy(f => f.Line)
            .ToList();

        // --- Secret verification (--verify) ---
        if (options.Verify)
        {
            findings = VerifySecrets(findings).GetAwaiter().GetResult();
        }

        // --- Baseline suppression (--baseline) ---
        if (options.UseBaseline)
        {
            var baseline = BaselineManager.Load(root);
            if (baseline is not null)
            {
                var newFindings = BaselineManager.FilterNew(findings, baseline);
                suppressedCount += findings.Count - newFindings.Count;
                findings = newFindings.ToList();
            }
        }

        // --- Write new baseline (--update-baseline) ---
        if (options.UpdateBaseline)
            BaselineManager.Save(root, findings);

        var (score, grade) = Scorer.Calculate(findings);

        return new ScanResult(root, findings, score, grade, filesScanned, sw.Elapsed, suppressedCount);
    }

    private static async Task<List<Finding>> VerifySecrets(List<Finding> findings)
    {
        var result = new List<Finding>(findings.Count);
        foreach (var f in findings)
        {
            if (f.RuleId.StartsWith("RWS-SEC") && f.RawValue is not null)
            {
                var verified = await SecretVerifier.VerifyAsync(f, f.RawValue);
                result.Add(f with { Verified = verified });
            }
            else
            {
                result.Add(f);
            }
        }
        return result;
    }
}
