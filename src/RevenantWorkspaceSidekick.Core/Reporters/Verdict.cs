namespace RevenantWorkspaceSidekick.Core.Reporters;

/// <summary>
/// The one-line human verdict that closes out a scan — the sentence a person
/// actually reads after they stop counting findings.
///
/// Lifted out of <see cref="ConsoleReporter"/> on 2026-07-30 so the desktop UI
/// can show the same words. Before that the roast text lived inside a private
/// method on the console reporter, which meant the UI could accept a "roast"
/// setting and then had no way to render it — the Roast checkbox in the window
/// was wired to nothing at all.
/// </summary>
public static class Verdict
{
    /// <summary>
    /// Grades that earn the roast without being asked for it.
    ///
    /// D and F only. Note the scoring makes this stricter than it looks: a single
    /// Critical is -25, so two Criticals alone already put a perfect project at 50,
    /// which is an F. By the time a scan roasts you, it has earned the right to.
    /// </summary>
    public static bool EarnsRoast(char grade) => grade is 'D' or 'F';

    /// <summary>
    /// The verdict line. <paramref name="roast"/> picks the opinionated wording;
    /// <paramref name="student"/> reframes shipping as turning work in.
    /// </summary>
    /// <remarks>
    /// Grades outside A-D fall to the F wording, including the placeholder '-'
    /// used before a scan has run. Callers are responsible for not asking for a
    /// verdict on a scan that has not happened.
    /// </remarks>
    public static string For(char grade, bool roast, bool student) => roast
        ? grade switch
        {
            'A' => "A. Either you actually know what you're doing, or you haven't checked in your AWS keys yet. Stay vigilant.",
            'B' => "B. Solid work. Still wouldn't trust this on a network that matters, but you're not the worst vibe-coder I've seen today.",
            'C' => "C. Your AI wrote this. You can tell because it works in demos and falls apart under any real threat model.",
            'D' => "D. This app is one misconfigured registry key away from becoming a conference talk case study.",
            _ => student
                ? "F. This isn't a homework submission. This is an incident report. Do not turn this in."
                : "F. This isn't a security audit. This is an incident report for something that hasn't happened yet."
        }
        : grade switch
        {
            'A' => student ? "Clean build. Ready to turn in." : "Clean build. Ship it.",
            'B' => student ? "A few rough edges. Worth a look before you turn it in." : "A few rough edges. Worth a look before release.",
            'C' => student ? "Some real issues here. Don't turn this in without addressing the highs." : "Some real issues here. Don't ship without addressing the highs.",
            'D' => student ? "Significant findings. This needs work before you submit it." : "Significant findings. This needs work before it goes anywhere near production.",
            _ => student ? "Do not turn this in." : "Do not ship this."
        };
}
