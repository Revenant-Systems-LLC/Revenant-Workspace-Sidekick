using System.Collections.Concurrent;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Core;

public static class RuleEngine
{
    public static ScanResult Scan(IReadOnlyList<IRule> rules, ScanOptions options)
    {
        var root = System.IO.Path.GetFullPath(options.ScanPath);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allFindings = new ConcurrentBag<Finding>();
        var filesScanned = 0;
        var suppressedCount = 0;

        var files = FileWalker.Enumerate(root, options.Includes, options.Excludes).ToList();

        Parallel.ForEach(files, file =>
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

        sw.Stop();

        // Dedup: when multiple rules in the same group fire on the same file+line,
        // keep only the highest-severity finding. Skip null-line findings (XML rules
        // can't reliably determine position and should never be collapsed).
        var filtered = allFindings.Where(f => f.Severity >= options.MinSeverity);
        var deduped = filtered
            .Where(f => f.Line.HasValue)
            .GroupBy(f => (f.File, f.Line!.Value, RuleGroup(f.RuleId)))
            .Select(g => g.MaxBy(f => f.Severity)!)
            .Concat(filtered.Where(f => !f.Line.HasValue));

        var findings = deduped
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.File)
            .ThenBy(f => f.Line)
            .ToList();

        var (score, grade) = Scorer.Calculate(findings);

        static string RuleGroup(string ruleId)
        {
            // "RWS-SEC-001" → "RWS-SEC"
            var first = ruleId.IndexOf('-');
            if (first < 0) return ruleId;
            var second = ruleId.IndexOf('-', first + 1);
            return second >= 0 ? ruleId[..second] : ruleId;
        }

        return new ScanResult(root, findings, score, grade, filesScanned, sw.Elapsed, suppressedCount);
    }
}
