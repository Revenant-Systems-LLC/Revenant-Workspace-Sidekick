using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core.Reporters;

public sealed class ConsoleReporter(bool roastMode = false, bool studentMode = false) : IReporter
{
    public void Report(ScanResult result, TextWriter output)
    {
        var prev = Console.ForegroundColor;

        WriteBanner(output);
        output.WriteLine($"  Scan root : {result.ScanRoot}");
        output.WriteLine($"  Files     : {result.FilesScanned}");
        output.WriteLine($"  Duration  : {result.Duration.TotalSeconds:F2}s");
        output.WriteLine();

        if (result.Findings.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            output.WriteLine(roastMode && !studentMode
                ? "  No findings. Either you're genuinely good at this or your AI got lucky. Either way, don't get smug."
                : "  No findings. Clean scan.");
            Console.ForegroundColor = prev;
            output.WriteLine();
        }
        else
        {
            foreach (var finding in result.Findings)
                WriteFinding(finding, output, prev, studentMode);
        }

        WriteSummary(result, output, prev);
        Console.ForegroundColor = prev;
    }

    private static void WriteBanner(TextWriter output)
    {
        output.WriteLine();
        output.WriteLine("  RWS — Revenant Workspace Sidekick Scanner");
        output.WriteLine("  " + new string('─', 50));
        output.WriteLine();
    }

    private static void WriteFinding(Finding finding, TextWriter output, ConsoleColor prev, bool studentMode)
    {
        Console.ForegroundColor = SeverityColor(finding.Severity);
        output.WriteLine($"[{finding.Severity.ToString().ToUpperInvariant()}] {finding.RuleId}  {finding.Title}");
        Console.ForegroundColor = prev;
        output.WriteLine($"File: {finding.File}{(finding.Line.HasValue ? $":{finding.Line}" : "")}");
        if (!string.IsNullOrWhiteSpace(finding.RedactedSnippet))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            output.WriteLine($"  Matched: {finding.RedactedSnippet}");
            Console.ForegroundColor = prev;
        }
        output.WriteLine();
        output.WriteLine("Why this matters:");
        output.WriteLine($"  {finding.Why}");
        output.WriteLine();
        output.WriteLine("Fix:");
        output.WriteLine($"  {finding.Fix}");
        
        if (!string.IsNullOrWhiteSpace(finding.Example))
        {
            output.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            output.WriteLine("Example (Better Way):");
            Console.ForegroundColor = prev;
            output.WriteLine(finding.Example);
        }
        
        output.WriteLine();
        output.WriteLine("  " + new string('·', 50));
        output.WriteLine();
    }

    private void WriteSummary(ScanResult result, TextWriter output, ConsoleColor prev)
    {
        var counts = result.Findings
            .GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        output.WriteLine("  " + new string('═', 50));
        output.WriteLine();

        Console.ForegroundColor = GradeColor(result.Grade);
        output.WriteLine($"  Score : {result.Score}/100   Grade : {result.Grade}");
        Console.ForegroundColor = prev;
        if (result.SuppressedCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            output.WriteLine($"  ({result.SuppressedCount} suppressed via RWS-suppress)");
            Console.ForegroundColor = prev;
        }
        output.WriteLine();

        void Count(Severity s)
        {
            var n = counts.GetValueOrDefault(s, 0);
            if (n == 0) return;
            Console.ForegroundColor = SeverityColor(s);
            output.WriteLine($"  {s,-10} {n}");
            Console.ForegroundColor = prev;
        }

        Count(Severity.Critical);
        Count(Severity.High);
        Count(Severity.Medium);
        Count(Severity.Low);
        Count(Severity.Info);

        output.WriteLine();
        WriteRoast(result, output, roastMode, studentMode);
        output.WriteLine();
    }

    // Wording lives in Verdict so the desktop UI can print the same sentence.
    private static void WriteRoast(ScanResult result, TextWriter output, bool hard, bool student)
        => output.WriteLine($"  {Verdict.For(result.Grade, hard, student)}");

    private static ConsoleColor SeverityColor(Severity s) => s switch
    {
        Severity.Critical => ConsoleColor.Red,
        Severity.High => ConsoleColor.DarkYellow,
        Severity.Medium => ConsoleColor.Yellow,
        Severity.Low => ConsoleColor.Gray,
        Severity.Info => ConsoleColor.Cyan,
        _ => ConsoleColor.White
    };

    private static ConsoleColor GradeColor(char grade) => grade switch
    {
        'A' => ConsoleColor.Green,
        'B' => ConsoleColor.Cyan,
        'C' => ConsoleColor.Yellow,
        'D' => ConsoleColor.DarkYellow,
        _ => ConsoleColor.Red
    };
}
