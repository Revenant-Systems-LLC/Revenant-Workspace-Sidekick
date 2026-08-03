using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RevenantWorkspaceSidekick.Ui;

public partial class MainWindow : Window
{
    // DWM window attribute: paint the title bar / caption in dark mode. Without
    // this the caption renders in the user's Windows accent color, which on a
    // fully obsidian app looks like the theme stops at the frame.
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaBorderColor = 34;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += ApplyDarkCaption;
    }

    // The animated background and its three MediaElement handlers were removed
    // 2026-07-30. It relied on wmp.dll, which is absent from this Windows install
    // and would have to be added by every customer as an optional Windows feature
    // just to see a background move. Dave scrapped it rather than ship that.
    // The background is now a single static image, set in MainWindow.xaml.

    private void ApplyDarkCaption(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0)
            return;

        // Best effort only. Older Windows builds return a failure HRESULT, in
        // which case the caption just stays default and nothing else breaks.
        var enabled = 1;
        DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));

        // COLORREF is 0x00BBGGRR, not RGB. Obsidian #050506 -> 0x00060505.
        var caption = 0x00060505;
        DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(int));

        // Gold hairline frame, #7A633E -> 0x003E637A.
        var border = 0x003E637A;
        DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(int));
    }
}
