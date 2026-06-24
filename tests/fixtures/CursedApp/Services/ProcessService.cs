using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace CursedApp.Services;

// RWS-EXEC-001: Process.Start with non-literal argument
// RWS-EXEC-002: UseShellExecute = true
// RWS-EXEC-003: Assembly.LoadFrom with non-literal path
// RWS-EXEC-004: URI handler registration
public class ProcessService
{
    public void OpenFile(string userProvidedPath)
    {
        // RWS-EXEC-001 + RWS-EXEC-002
        var psi = new ProcessStartInfo
        {
            FileName = userProvidedPath,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    public void RunTool(string toolName)
    {
        // RWS-EXEC-001
        Process.Start(toolName);
    }

    public void RunCommand(string userCommand)
    {
        // RWS-EXEC-005: interpolated string passed to Process.Start
        Process.Start("cmd.exe", $"/c {userCommand}");
    }

    public void LoadPlugin(string pluginPath)
    {
        // RWS-EXEC-003
        var asm = Assembly.LoadFrom(pluginPath);
        var type = asm.GetType("Plugin.Main");
        type?.GetMethod("Run")?.Invoke(null, null);
    }

    public void RegisterUriHandler()
    {
        // RWS-EXEC-004
        using var key = Registry.ClassesRoot.OpenSubKey(@"cursedapp\shell\open\command", true);
        key?.SetValue("", $"\"{Environment.ProcessPath}\" \"%1\"");
        Registry.SetValue(@"HKEY_CLASSES_ROOT\cursedapp", "URL Protocol", "");
    }
}
