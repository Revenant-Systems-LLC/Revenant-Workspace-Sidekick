using System.Windows.Media;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Ui.Infrastructure;

/// <summary>
/// Severity and grade colors for the Revenant house theme (obsidian / gold /
/// turquoise). These values are the SAME ones <c>HtmlReporter</c> writes into an
/// exported report, so a finding reads the same color on screen as it does in
/// the HTML a user sends to someone else.
///
/// Brand turquoise (#3EC2C7) is deliberately NOT used for any severity. It is
/// reserved for chrome, so an accent never gets mistaken for a finding.
///
/// If these change, change <c>HtmlReporter.Css()</c> and
/// <c>Theme/RevenantTheme.xaml</c> to match. Three places, one palette.
/// </summary>
public static class SeverityBrushes
{
    public static readonly Brush Critical = Frozen(0xC8, 0x70, 0x70);
    public static readonly Brush High     = Frozen(0xC0, 0x70, 0x50);
    public static readonly Brush Medium   = Frozen(0xD4, 0xA2, 0x57);
    public static readonly Brush Low      = Frozen(0x7F, 0xA6, 0xC9);
    public static readonly Brush InfoOrDefault = Frozen(0xA8, 0xB5, 0xC8);

    /// <summary>Jade, for a clean result. Matches Warden's success color.</summary>
    public static readonly Brush Jade = Frozen(0x3A, 0xD5, 0xA1);

    // 15% washes, painted behind a badge or card so the color reads without
    // shouting over the obsidian.
    public static readonly Brush CriticalWash = Frozen(0x26, 0xC8, 0x70, 0x70);
    public static readonly Brush HighWash     = Frozen(0x26, 0xC0, 0x70, 0x50);
    public static readonly Brush MediumWash   = Frozen(0x26, 0xD4, 0xA2, 0x57);
    public static readonly Brush LowWash      = Frozen(0x26, 0x7F, 0xA6, 0xC9);
    public static readonly Brush InfoWash     = Frozen(0x26, 0xA8, 0xB5, 0xC8);

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
