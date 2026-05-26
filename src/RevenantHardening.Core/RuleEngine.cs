using System.Collections.Concurrent;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Core;

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
                        RuleId: "RSH-INTERNAL",
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

        var findings = allFindings
            .Where(f => f.Severity >= options.MinSeverity)
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.File)
            .ThenBy(f => f.Line)
            .ToList();

        var (score, grade) = Scorer.Calculate(findings);

        return new ScanResult(root, findings, score, grade, filesScanned, sw.Elapsed, suppressedCount);
    }
}
