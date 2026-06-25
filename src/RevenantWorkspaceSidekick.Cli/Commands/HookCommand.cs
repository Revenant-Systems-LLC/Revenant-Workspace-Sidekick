namespace RevenantWorkspaceSidekick.Cli.Commands;

public static class HookCommand
{
    private const string HookBody = """
        #!/bin/sh
        # RWS pre-commit hook — installed by: rws hook install
        # Scans staged/changed files before every commit.
        rws scan --diff --severity high --format console
        exit $?
        """;

    public static int Execute(string[] args)
    {
        var subcommand = args.Length > 0 ? args[0] : "";
        var gitRoot = FindGitRoot(Directory.GetCurrentDirectory());

        if (gitRoot is null)
        {
            Console.Error.WriteLine("Error: not inside a git repository");
            return 1;
        }

        var hookPath = Path.Combine(gitRoot, ".git", "hooks", "pre-commit");

        return subcommand switch
        {
            "install" => Install(hookPath),
            "uninstall" => Uninstall(hookPath),
            _ => PrintUsage()
        };
    }

    private static int Install(string hookPath)
    {
        if (File.Exists(hookPath))
        {
            var existing = File.ReadAllText(hookPath);
            if (existing.Contains("RWS pre-commit hook"))
            {
                Console.WriteLine("RWS pre-commit hook is already installed.");
                return 0;
            }
            Console.Error.WriteLine($"Error: a pre-commit hook already exists at {hookPath}");
            Console.Error.WriteLine("Remove it manually first, then re-run: rws hook install");
            return 1;
        }

        File.WriteAllText(hookPath, HookBody);

        // On non-Windows systems, chmod +x is needed; on Windows git-bash emulates this
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                var chmod = System.Diagnostics.Process.Start("chmod", $"+x \"{hookPath}\"");
                chmod?.WaitForExit();
            }
        }
        catch { /* chmod failure is non-fatal on Windows */ }

        Console.WriteLine($"RWS pre-commit hook installed at: {hookPath}");
        Console.WriteLine("Every commit will now run: rws scan --diff --severity high");
        return 0;
    }

    private static int Uninstall(string hookPath)
    {
        if (!File.Exists(hookPath))
        {
            Console.WriteLine("No pre-commit hook found — nothing to remove.");
            return 0;
        }

        var existing = File.ReadAllText(hookPath);
        if (!existing.Contains("RWS pre-commit hook"))
        {
            Console.Error.WriteLine("Error: the existing pre-commit hook was not installed by RWS.");
            Console.Error.WriteLine("Remove it manually to avoid losing unrelated hook logic.");
            return 1;
        }

        File.Delete(hookPath);
        Console.WriteLine("RWS pre-commit hook removed.");
        return 0;
    }

    private static string? FindGitRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static int PrintUsage()
    {
        Console.Error.WriteLine("""

            Usage: rws hook <subcommand>

            Subcommands:
              install      Install a pre-commit hook that runs: rws scan --diff --severity high
              uninstall    Remove the RWS pre-commit hook

            The hook is written to .git/hooks/pre-commit in the nearest git repository.
            """);
        return 1;
    }
}
