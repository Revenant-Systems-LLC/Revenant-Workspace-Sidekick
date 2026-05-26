using System.Text.RegularExpressions;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Core;

/// <summary>
/// Evaluates inline suppression annotations.
/// Syntax: add  rsh-suppress: RSH-XXXX-NNN  (any optional trailing text)
/// in a comment on the flagged line or the line immediately above it.
/// Works with any comment style — C# //, XML &lt;!-- --&gt;, etc.
/// </summary>
public static class SuppressionChecker
{
    private static readonly Regex Pattern = new(
        @"rsh-suppress:\s*(\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsSuppressed(string content, Finding finding)
    {
        var lines = content.Split('\n');

        if (finding.Line is { } line && line >= 1)
        {
            // Check the finding line and the line immediately above (1-indexed → 0-indexed).
            Span<int> indices = [line - 1, line - 2];
            foreach (var idx in indices)
            {
                if ((uint)idx >= (uint)lines.Length)
                    continue;

                if (MatchesRule(lines[idx], finding.RuleId))
                    return true;
            }
            return false;
        }

        // No line number (e.g. XML manifest rules) — accept a file-level suppression anywhere in the file.
        foreach (var l in lines)
        {
            if (MatchesRule(l, finding.RuleId))
                return true;
        }
        return false;
    }

    private static bool MatchesRule(string line, string ruleId)
    {
        var match = Pattern.Match(line);
        return match.Success &&
               match.Groups[1].Value.Equals(ruleId, StringComparison.OrdinalIgnoreCase);
    }
}
