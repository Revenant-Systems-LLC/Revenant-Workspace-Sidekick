using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;
using RevenantWorkspaceScout.Core.Reporters;

namespace RevenantWorkspaceScout.Cli.Commands;

public static class ScanCommand
{
    public static int Execute(string[] args, IReadOnlyList<IRule> rules)
    {
        var options = ParseOptions(args);
        if (options is null)
            return 1;

        if (!Directory.Exists(options.ScanPath) && !File.Exists(options.ScanPath))
        {
            Console.Error.WriteLine($"Error: path does not exist: {options.ScanPath}");
            return 1;
        }

        var result = RuleEngine.Scan(rules, options);

        IReporter reporter = options.Format.ToLowerInvariant() switch
        {
            "json" => new JsonReporter(),
            "html" => new HtmlReporter(),
            _ => new ConsoleReporter(options.Roast, options.StudentMode)
        };

        if (options.OutputFile is not null)
        {
            using var file = new StreamWriter(options.OutputFile, append: false);
            reporter.Report(result, file);
        }
        else
        {
            reporter.Report(result, Console.Out);
        }

        return result.Grade is 'A' or 'B' ? 0 : 1;
    }

    private static ScanOptions? ParseOptions(string[] args)
    {
        var path = ".";
        var format = "console";
        string? outputFile = null;
        var offline = false;
        var roast = false;
        var studentMode = false;
        var minSeverity = Severity.Low;
        var includes = new List<string>();
        var excludes = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (!arg.StartsWith("--"))
            {
                path = arg;
                continue;
            }

            switch (arg)
            {
                case "--offline":
                    offline = true;
                    break;
                case "--roast":
                    roast = true;
                    break;
                case "--student":
                    studentMode = true;
                    if (minSeverity > Severity.Info)
                        minSeverity = Severity.Info;
                    break;
                case "--format" when i + 1 < args.Length:
                    format = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--severity" when i + 1 < args.Length:
                    if (!TryParseSeverity(args[++i], out minSeverity))
                    {
                        Console.Error.WriteLine($"Error: unknown severity '{args[i]}'. Use: info, low, medium, high, critical");
                        return null;
                    }
                    break;
                case "--include" when i + 1 < args.Length:
                    includes.Add(args[++i]);
                    break;
                case "--exclude" when i + 1 < args.Length:
                    excludes.Add(args[++i]);
                    break;
                default:
                    Console.Error.WriteLine($"Unknown flag: {arg}");
                    PrintUsage();
                    return null;
            }
        }

        _ = offline; // reserved for future use

        return new ScanOptions(path, format, outputFile, offline, roast, minSeverity,
            [.. includes], [.. excludes], studentMode);
    }

    private static bool TryParseSeverity(string value, out Severity severity)
    {
        severity = Severity.Low;
        return Enum.TryParse(value, ignoreCase: true, out severity);
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("""

            Usage: rws scan [path] [options]

            Options:
              --format console|json|html   Output format (default: console)
              --output <path>              Write output to file
              --severity low|medium|high|critical
                                           Minimum severity to report (default: low)
              --roast                      Opinionated summary wording
              --offline                    Reserved; no effect in v0.1
              --include <ext>              Additional file extension to scan
              --exclude <segment>          Path segment to exclude
            """);
    }
}
