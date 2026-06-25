using Microsoft.Win32;

namespace CursedApp.Services;

// RWS-REG-001: Registry.LocalMachine access
// RWS-REG-002: OpenSubKey with writable: true
// RWS-REG-003: HKLM write with no elevation guard
public class RegistryService
{
    public void WriteAppSetting(string key, string value)
    {
        var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\CursedApp", writable: true);
        regKey?.SetValue(key, value);
    }

    public string? ReadAppSetting(string key)
    {
        var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\CursedApp");
        return regKey?.GetValue(key)?.ToString();
    }

    public void RegisterApp()
    {
        using var hklm = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\CursedApp", true);
        hklm?.SetValue("Installed", "true");
        hklm?.SetValue("Version", "1.0.0");
    }

    public void FixPermissions()
    {
        // RWS-REG-004: SetAccessControl on HKLM key
        using var hklm = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\CursedApp", true);
        var acl = hklm?.GetAccessControl();
        if (acl is not null)
            hklm?.SetAccessControl(acl);
    }
}
