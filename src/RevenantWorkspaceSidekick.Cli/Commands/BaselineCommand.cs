using RevenantWorkspaceSidekick.Core;
using RevenantWorkspaceSidekick.Rules;

namespace RevenantWorkspaceSidekick.Cli.Commands;

/// <summary>
/// rws baseline update [path]  — scan and write .rws-baseline.json
/// rws baseline status [path]  — show how many findings are in the baseline
/// rws baseline clear [path]   — delete .rws-baseline.json
/// </summary>
public static class BaselineCommand
{
    public static int Execute(string[] args)
    {
        var subcommand = args.Length > 0 ? args[0] : "";
        var path = args.Length > 1 ? args[1] : ".";

        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Error: path does not exist: {path}");
            return 1;
        }

        return subcommand switch
        {
            "update" => Update(path),
            "status" => Status(path),
            "clear" => Clear(path),
            _ => PrintUsage()
        };
    }

    private static int Update(string path)
    {
        Console.WriteLine($"Scanning {path} to build baseline...");
        var options = ScanOptions.Default(path) with { UpdateBaseline = true };
        var result = RuleEngine.Scan(RuleRegistry.All, options);
        var baselinePath = BaselineManager.BaselinePath(System.IO.Path.GetFullPath(path));
        Console.WriteLine($"Baseline written: {baselinePath}");
        Console.WriteLine($"  {result.Findings.Count} findings recorded as known/accepted");
        Console.WriteLine("Future scans with --baseline will suppress these and report only new findings.");
        return 0;
    }

    private static int Status(string path)
    {
        var root = System.IO.Path.GetFullPath(path);
        var baseline = BaselineManager.Load(root);
        if (baseline is null)
        {
            Console.WriteLine("No baseline found. Run: rws baseline update [path]");
            return 0;
        }
        Console.WriteLine($"Baseline: {BaselineManager.BaselinePath(root)}");
        Console.WriteLine($"  Created: {baseline.CreatedAt}");
        Console.WriteLine($"  Entries: {baseline.Entries.Count} known findings");
        return 0;
    }

    private static int Clear(string path)
    {
        var root = System.IO.Path.GetFullPath(path);
        var baselinePath = BaselineManager.BaselinePath(root);
        if (!File.Exists(baselinePath))
        {
            Console.WriteLine("No baseline file found.");
            return 0;
        }
        File.Delete(baselinePath);
        Console.WriteLine($"Baseline cleared: {baselinePath}");
        return 0;
    }

    private static int PrintUsage()
    {
        Console.Error.WriteLine("""

            Usage: rws baseline <subcommand> [path]

            Subcommands:
              update [path]   Scan and write .rws-baseline.json (suppresses findings in future --baseline scans)
              status [path]   Show how many findings are in the current baseline
              clear  [path]   Delete the .rws-baseline.json file

            Typical workflow:
              rws baseline update .    # accept current state as baseline
              rws scan --baseline .    # future scans only report NEW findings
            """);
        return 1;
    }
}
