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
        Loaded += (_, _) => BackgroundVideo.Play();
    }

    // MediaElement has no built-in loop. Seeking back to zero on MediaEnded is
    // the standard approach and is seamless here because the clip was cut on a
    // matched frame pair.
    private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        BackgroundVideo.Position = TimeSpan.Zero;
        BackgroundVideo.Play();
    }

    private void BackgroundVideo_MediaOpened(object sender, RoutedEventArgs e)
    {
        BackgroundFallback.Visibility = Visibility.Collapsed;
    }

    // If the video can't open (missing codec on a stripped-down Windows install,
    // corrupt file), fall back to the still image rather than showing nothing.
    private void BackgroundVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        BackgroundVideo.Visibility = Visibility.Collapsed;
        BackgroundFallback.Visibility = Visibility.Visible;
    }

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
