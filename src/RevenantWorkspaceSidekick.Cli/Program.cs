using RevenantWorkspaceSidekick.Cli.Commands;
using RevenantWorkspaceSidekick.Rules;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("""
        RWS — Revenant Workspace Sidekick Scanner v0.68
        Audit your AI-coded app before you ship it.

        Commands:
          rws scan [path] [options]    Scan a directory for security and hardening issues
          rws baseline <sub> [path]   Manage the known-findings baseline
          rws hook install|uninstall  Install/remove the git pre-commit hook

        Run 'rws scan --help' for scan options.
        Run 'rws baseline' or 'rws hook' for subcommand help.
        """);
    return 0;
}

return args[0] switch
{
    "scan" => ScanCommand.Execute(args[1..], RuleRegistry.All),
    "baseline" => BaselineCommand.Execute(args[1..]),
    "hook" => HookCommand.Execute(args[1..]),
    _ => UnknownCommand(args[0])
};

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}. Try: rws scan | rws baseline | rws hook");
    return 1;
}
