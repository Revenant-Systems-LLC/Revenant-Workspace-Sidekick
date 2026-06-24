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

    private static void WriteRoast(ScanResult result, TextWriter output, bool hard, bool student)
    {
        var msg = hard
            ? result.Grade switch
            {
                'A' => "A. Either you actually know what you're doing, or you haven't checked in your AWS keys yet. Stay vigilant.",
                'B' => "B. Solid work. Still wouldn't trust this on a network that matters, but you're not the worst vibe-coder I've seen today.",
                'C' => "C. Your AI wrote this. You can tell because it works in demos and falls apart under any real threat model.",
                'D' => "D. This app is one misconfigured registry key away from becoming a conference talk case study.",
                _ => student 
                    ? "F. This isn't a homework submission. This is an incident report. Do not turn this in." 
                    : "F. This isn't a security audit. This is an incident report for something that hasn't happened yet."
            }
            : result.Grade switch
            {
                'A' => student ? "Clean build. Ready to turn in." : "Clean build. Ship it.",
                'B' => student ? "A few rough edges. Worth a look before you turn it in." : "A few rough edges. Worth a look before release.",
                'C' => student ? "Some real issues here. Don't turn this in without addressing the highs." : "Some real issues here. Don't ship without addressing the highs.",
                'D' => student ? "Significant findings. This needs work before you submit it." : "Significant findings. This needs work before it goes anywhere near production.",
                _ => student ? "Do not turn this in." : "Do not ship this."
            };
        output.WriteLine($"  {msg}");
    }

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
