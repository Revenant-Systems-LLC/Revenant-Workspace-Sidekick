using RevenantHardening.Cli.Commands;
using RevenantHardening.Rules;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("""
        RSH — Revenant Hardening Scanner v0.68
        Audit your AI-coded Windows app before you ship it.

        Commands:
          rsh scan [path] [options]    Scan a directory for hardening issues

        Run 'rsh scan --help' for scan options.
        """);
    return 0;
}

if (args[0] == "scan")
    return ScanCommand.Execute(args[1..], RuleRegistry.All);

Console.Error.WriteLine($"Unknown command: {args[0]}. Try: rsh scan");
return 1;
