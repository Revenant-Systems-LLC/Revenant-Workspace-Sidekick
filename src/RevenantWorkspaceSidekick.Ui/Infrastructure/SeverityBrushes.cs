using System.Windows.Media;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Ui.Infrastructure;

/// <summary>
/// Severity and grade colors for the Revenant house theme (obsidian / gold /
/// turquoise). These values are the SAME ones <c>HtmlReporter</c> writes into an
/// exported report, so a finding reads the same color on screen as it does in
/// the HTML a user sends to someone else.
///
/// Set by Dave 2026-07-30 from a colour sketch drawn over a screenshot of the
/// running app — light blue, yellow, orange, red, deep crimson.
///
/// A pure heat ramp, except Critical goes DOWN into crimson instead of further
/// up. It is the only value that gets darker rather than hotter, which is what
/// tells it apart from High at a glance rather than by hue alone.
///
/// Earlier passes ran teal Low and purple Critical. Both are dead; do not
/// reconstruct them.
///
/// If these change, change <c>HtmlReporter.Css()</c> and
/// <c>Theme/RevenantTheme.xaml</c> to match. Three places, one palette.
/// </summary>
public static class SeverityBrushes
{
    public static readonly Brush Critical = Frozen(0x8C, 0x14, 0x36);
    public static readonly Brush High     = Frozen(0xDA, 0x2B, 0x26);
    public static readonly Brush Medium   = Frozen(0xF5, 0x9A, 0x20);
    public static readonly Brush Low      = Frozen(0xF0, 0xDE, 0x1B);
    public static readonly Brush InfoOrDefault = Frozen(0x7F, 0xA6, 0xC9);

    /// <summary>Jade, for a clean result. Matches Warden's success color.</summary>
    public static readonly Brush Jade = Frozen(0x3A, 0xD5, 0xA1);

    // 15% washes, painted behind a badge or card so the color reads without
    // shouting over the obsidian.
    public static readonly Brush CriticalWash = Frozen(0x26, 0x8C, 0x14, 0x36);
    public static readonly Brush HighWash     = Frozen(0x26, 0xDA, 0x2B, 0x26);
    public static readonly Brush MediumWash   = Frozen(0x26, 0xF5, 0x9A, 0x20);
    public static readonly Brush LowWash      = Frozen(0x26, 0xF0, 0xDE, 0x1B);
    public static readonly Brush InfoWash     = Frozen(0x26, 0x7F, 0xA6, 0xC9);

    public static Brush For(Severity severity) => severity switch
    {
        Severity.Critical => Critical,
        Severity.High     => High,
        Severity.Medium   => Medium,
        Severity.Low      => Low,
        _                 => InfoOrDefault,
    };

    public static Brush WashFor(Severity severity) => severity switch
    {
        Severity.Critical => CriticalWash,
        Severity.High     => HighWash,
        Severity.Medium   => MediumWash,
        Severity.Low      => LowWash,
        _                 => InfoWash,
    };

    // 35% fills, for a severity card that is currently acting as the grid filter.
    // Mirrors the Sev*Active brushes in Theme/RevenantTheme.xaml.
    public static readonly Brush CriticalActive = Frozen(0x59, 0x8C, 0x14, 0x36);
    public static readonly Brush HighActive     = Frozen(0x59, 0xDA, 0x2B, 0x26);
    public static readonly Brush MediumActive   = Frozen(0x59, 0xF5, 0x9A, 0x20);
    public static readonly Brush LowActive      = Frozen(0x59, 0xF0, 0xDE, 0x1B);
    public static readonly Brush InfoActive     = Frozen(0x59, 0x7F, 0xA6, 0xC9);

    public static Brush ActiveFor(Severity severity) => severity switch
    {
        Severity.Critical => CriticalActive,
        Severity.High     => HighActive,
        Severity.Medium   => MediumActive,
        Severity.Low      => LowActive,
        _                 => InfoActive,
    };

    /// <summary>Muted gunmetal, used for the grade ring before a scan has run.</summary>
    public static readonly Brush Unscored = Frozen(0x5B, 0x6B, 0x7A);

    /// <summary>
    /// Grade ring / numeral color. Mirrors HtmlReporter's grade switch, with one
    /// addition: an un-run scan shows muted, not failing red.
    /// </summary>
    public static Brush ForGrade(char grade) => grade switch
    {
        '-' => Unscored,
        'A' => Jade,
        'B' => Frozen(0x70, 0xC8, 0x70),
        'C' => Medium,
        'D' => High,
        _   => Critical,
    };

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Brush Frozen(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }
}
