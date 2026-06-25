using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Core.Reporters;

namespace RevenantWorkspaceSidekick.Cli.Commands;

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

        if (options.DiffOnly && !GitHelper.IsGitRepo(System.IO.Path.GetFullPath(options.ScanPath)))
        {
            Console.Error.WriteLine("Warning: --diff requires a git repository. Falling back to full scan.");
        }

        if (options.Verify)
            Console.Error.WriteLine("RWS: --verify enabled — making outbound HTTP calls to confirm live credentials");

        var result = RuleEngine.Scan(rules, options);

        IReporter reporter = options.Format.ToLowerInvariant() switch
        {
            "json" => new JsonReporter(),
            "html" => new HtmlReporter(),
            "sarif" => new SarifReporter(),
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

        if (options.UpdateBaseline)
        {
            var baselinePath = BaselineManager.BaselinePath(System.IO.Path.GetFullPath(options.ScanPath));
            Console.Error.WriteLine($"RWS: baseline written to {baselinePath}");
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
        var diffOnly = false;
        var scanHistory = false;
        var historyDepth = 100;
        var auditDeps = false;
        var verify = false;
        var updateBaseline = false;
        var useBaseline = false;

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
                case "--diff":
                    diffOnly = true;
                    break;
                case "--history":
                    scanHistory = true;
                    break;
                case "--history-depth" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out historyDepth) || historyDepth < 1)
                    {
                        Console.Error.WriteLine("Error: --history-depth must be a positive integer");
                        return null;
                    }
                    break;
                case "--audit-deps":
                    auditDeps = true;
                    break;
                case "--verify":
                    verify = true;
                    break;
                case "--update-baseline":
                    updateBaseline = true;
                    break;
                case "--baseline":
                    useBaseline = true;
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

        _ = offline;

        return new ScanOptions(path, format, outputFile, offline, roast, minSeverity,
            [.. includes], [.. excludes], studentMode,
            diffOnly, scanHistory, historyDepth, auditDeps, verify, updateBaseline, useBaseline);
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
              --format console|json|html|sarif   Output format (default: console)
              --output <path>                    Write output to file
              --severity low|medium|high|critical
                                                 Minimum severity to report (default: low)
              --roast                            Opinionated summary wording
              --student                          Include Info-level findings with educational context
              --diff                             Scan only git-changed files (fast CI mode)
              --history                          Scan git log blobs for deleted secrets
              --history-depth <N>                Number of commits to scan in history (default: 100)
              --audit-deps                       Audit NuGet packages for CVEs (requires dotnet SDK)
              --verify                           Confirm live credentials via outbound HTTP calls
              --baseline                         Suppress findings already in .rws-baseline.json
              --update-baseline                  Write current findings to .rws-baseline.json
              --offline                          Reserved; no effect in current version
              --include <ext>                    Additional file extension to scan
              --exclude <segment>                Path segment to exclude
            """);
    }
}
