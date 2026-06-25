using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace CursedApp.Services;

// RWS-PINVOKE-001: DllImport without CharSet=Unicode on string-param function
// RWS-PINVOKE-002: DllImport with non-literal DLL name
// RWS-PINVOKE-003: Dangerous impersonation API
// RWS-ACL-001: File.SetAccessControl
// RWS-ACL-002: FileSystemAccessRule with "Everyone"
public class NativeService
{
    private const string KernelDll = "kernel32.dll";

    // RWS-PINVOKE-001: no CharSet, has string parameter
    [DllImport("kernel32.dll")]
    private static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

    // RWS-PINVOKE-002: DLL name from variable, not a literal
    [DllImport(KernelDll)]
    private static extern IntPtr LoadLibraryDynamic(string lpLibFileName);

    // RWS-PINVOKE-003: privilege escalation API
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

    // RWS-PINVOKE-003: impersonation API
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public int PrivilegeCount; public long Luid; public int Attributes; }

    public void GrantEveryoneAccess(string path)
    {
        // RWS-ACL-001: filesystem ACL modification
        var dirInfo = new System.IO.DirectoryInfo(path);
        var security = dirInfo.GetAccessControl();

        // RWS-ACL-002: FileSystemAccessRule with "Everyone"
        security.AddAccessRule(new FileSystemAccessRule(
            "Everyone",
            FileSystemRights.FullControl,
            AccessControlType.Allow));

        dirInfo.SetAccessControl(security);
    }
}
