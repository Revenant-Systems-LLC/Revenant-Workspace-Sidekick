using System.Net;
using System.Reflection;
using System.Text;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core.Reporters;

public sealed class HtmlReporter : IReporter
{
    private const string SiteUrl      = "https://www.revenantsystems.net";
    private const string GithubOrg    = "https://github.com/Revenant-Systems-LLC";
    private const string EchoUrl      = "https://github.com/Revenant-Systems-LLC/Revenant-Echo";
    private const string SortingHatUrl = "https://github.com/Revenant-Systems-LLC/Revenant-Sorting-Hat";
    private const string RelayUrl     = "https://github.com/Revenant-Systems-LLC/Revenant-Relay";

    public void Report(ScanResult result, TextWriter output)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>RWS Report — {WebUtility.HtmlEncode(result.ScanRoot)}</title>");
        sb.AppendLine(Css());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        AppendHeader(sb);
        AppendHero(sb, result);

        sb.AppendLine("<main class=\"container\">");
        AppendFindings(sb, result);
        sb.AppendLine("</main>");

        AppendFooter(sb);

        sb.AppendLine(InteractiveJs());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        output.Write(sb.ToString());
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("<header class=\"site-header\">");
        sb.AppendLine("  <div class=\"site-header__inner\">");
        sb.AppendLine("    <div class=\"site-header__brand\">");
        sb.AppendLine($"      <img class=\"site-logo\" src=\"data:image/png;base64,{LoadIconBase64()}\" alt=\"Revenant Systems\" />");
        sb.AppendLine("      <div class=\"site-header__text\">");
        sb.AppendLine($"        <a href=\"{SiteUrl}\" class=\"brand-name\" target=\"_blank\" rel=\"noopener\">REVENANT SYSTEMS</a>");
        sb.AppendLine("        <span class=\"tool-name\">Workspace Sidekick</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <nav class=\"site-nav\">");
        sb.AppendLine($"      <a href=\"{SiteUrl}\" class=\"nav-link\" target=\"_blank\" rel=\"noopener\">revenantsystems.net</a>");
        sb.AppendLine($"      <a href=\"{GithubOrg}\" class=\"nav-link\" target=\"_blank\" rel=\"noopener\">GitHub</a>");
        sb.AppendLine("    </nav>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header>");
    }

    private static void AppendHero(StringBuilder sb, ScanResult result)
    {
        var (gradeColor, _) = result.Grade switch
        {
            'A' => ("#00c896", "rgba(0,200,150,0.12)"),
            'B' => ("#4caf50", "rgba(76,175,80,0.12)"),
            'C' => ("#f5c518", "rgba(245,197,24,0.12)"),
            'D' => ("#ff7c2b", "rgba(255,124,43,0.12)"),
            _   => ("#ff3c3c", "rgba(255,60,60,0.12)")
        };

        var arc = result.Score / 100.0 * 326.73;
        var bySeverity = result.Findings.GroupBy(f => f.Severity).ToDictionary(g => g.Key, g => g.Count());
        var critical = bySeverity.GetValueOrDefault(Severity.Critical, 0);
        var high     = bySeverity.GetValueOrDefault(Severity.High, 0);
        var medium   = bySeverity.GetValueOrDefault(Severity.Medium, 0);
        var low      = bySeverity.GetValueOrDefault(Severity.Low, 0);

        sb.AppendLine("<section class=\"hero\">");
        sb.AppendLine("  <div class=\"hero__inner\">");

        sb.AppendLine("    <div class=\"score-ring-wrap\">");
        sb.AppendLine($"      <svg class=\"score-ring\" viewBox=\"0 0 120 120\" width=\"120\" height=\"120\">");
        sb.AppendLine($"        <circle cx=\"60\" cy=\"60\" r=\"52\" fill=\"none\" stroke=\"#1a2535\" stroke-width=\"8\"/>");
        sb.AppendLine($"        <circle cx=\"60\" cy=\"60\" r=\"52\" fill=\"none\" stroke=\"{gradeColor}\" stroke-width=\"8\" stroke-dasharray=\"{arc:F1} 326.73\" stroke-linecap=\"round\" transform=\"rotate(-90 60 60)\"/>");
        sb.AppendLine($"        <text x=\"60\" y=\"56\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"#e8edf2\" font-size=\"28\" font-weight=\"900\" font-family=\"system-ui,sans-serif\">{result.Grade}</text>");
        sb.AppendLine($"        <text x=\"60\" y=\"76\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{gradeColor}\" font-size=\"12\" font-family=\"system-ui,sans-serif\">{result.Score}/100</text>");
        sb.AppendLine("      </svg>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <div class=\"hero__body\">");
        sb.AppendLine("      <p class=\"hero__label\">SECURITY AUDIT REPORT</p>");
        sb.AppendLine($"      <p class=\"hero__path\">{WebUtility.HtmlEncode(result.ScanRoot)}</p>");

        sb.AppendLine("      <div class=\"sev-chips\">");
        if (critical > 0) sb.AppendLine($"        <span class=\"sev-chip sev-chip--critical\"><span class=\"sev-chip__count\">{critical}</span> Critical</span>");
        if (high     > 0) sb.AppendLine($"        <span class=\"sev-chip sev-chip--high\"><span class=\"sev-chip__count\">{high}</span> High</span>");
        if (medium   > 0) sb.AppendLine($"        <span class=\"sev-chip sev-chip--medium\"><span class=\"sev-chip__count\">{medium}</span> Medium</span>");
        if (low      > 0) sb.AppendLine($"        <span class=\"sev-chip sev-chip--low\"><span class=\"sev-chip__count\">{low}</span> Low</span>");
        if (result.Findings.Count == 0) sb.AppendLine("        <span class=\"sev-chip sev-chip--clean\">&#10003; Clean</span>");
        sb.AppendLine("      </div>");

        sb.AppendLine("      <div class=\"hero__meta\">");
        sb.AppendLine($"        <span class=\"meta-item\"><span class=\"meta-label\">Files</span> {result.FilesScanned}</span>");
        sb.AppendLine($"        <span class=\"meta-sep\">|</span>");
        sb.AppendLine($"        <span class=\"meta-item\"><span class=\"meta-label\">Findings</span> {result.Findings.Count}</span>");
        sb.AppendLine($"        <span class=\"meta-sep\">|</span>");
        sb.AppendLine($"        <span class=\"meta-item\"><span class=\"meta-label\">Duration</span> {result.Duration.TotalSeconds:F2}s</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</section>");
    }

    private static void AppendFindings(StringBuilder sb, ScanResult result)
    {
        if (result.Findings.Count == 0)
        {
            sb.AppendLine("<div class=\"clean-state\">");
            sb.AppendLine("  <div class=\"clean-state__icon\">&#10003;</div>");
            sb.AppendLine("  <p class=\"clean-state__title\">No findings detected</p>");
            sb.AppendLine("  <p class=\"clean-state__sub\">Your project passed all active rules.</p>");
            sb.AppendLine("</div>");
            return;
        }

        sb.AppendLine("<div class=\"findings-toolbar\">");
        sb.AppendLine("  <div class=\"filter-btns\">");
        sb.AppendLine("    <button class=\"filter-btn active\" data-sev=\"all\" onclick=\"filterFindings('all')\">All</button>");
        foreach (var sev in new[] { Severity.Critical, Severity.High, Severity.Medium, Severity.Low, Severity.Info })
        {
            var sevClass = sev.ToString().ToLowerInvariant();
            var count = result.Findings.Count(f => f.Severity == sev);
            if (count > 0)
                sb.AppendLine($"    <button class=\"filter-btn filter-btn--{sevClass}\" data-sev=\"{sevClass}\" onclick=\"filterFindings('{sevClass}')\">{sev} <span class=\"filter-count\">{count}</span></button>");
        }
        sb.AppendLine("  </div>");
        sb.AppendLine("  <input class=\"search-box\" type=\"search\" placeholder=\"Search findings...\" oninput=\"searchFindings(this.value)\" aria-label=\"Search findings\" />");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"section-label\">FINDINGS</div>");
        sb.AppendLine("<div id=\"findings-list\">");

        foreach (var finding in result.Findings)
        {
            var sevClass  = finding.Severity.ToString().ToLowerInvariant();
            var findingId = $"f{Math.Abs(finding.GetHashCode())}";
            var searchText = WebUtility.HtmlEncode(
                (finding.Title + " " + finding.File + " " + finding.RuleId + " " + finding.Why).ToLowerInvariant());

            sb.AppendLine($"<article class=\"finding finding--{sevClass}\" data-severity=\"{sevClass}\" data-searchtext=\"{searchText}\">");

            sb.AppendLine($"  <div class=\"finding__header\" onclick=\"toggleFinding('{findingId}')\" role=\"button\" aria-expanded=\"true\" aria-controls=\"{findingId}\">");
            sb.AppendLine($"    <span class=\"sev-badge sev-badge--{sevClass}\">{finding.Severity.ToString().ToUpperInvariant()}</span>");
            sb.AppendLine($"    <span class=\"rule-id\">{WebUtility.HtmlEncode(finding.RuleId)}</span>");
            sb.AppendLine($"    <span class=\"rule-title\">{WebUtility.HtmlEncode(finding.Title)}</span>");
            sb.AppendLine("    <div class=\"finding__tags\">");
            if (finding.Verified == true)  sb.AppendLine("      <span class=\"tag tag--live\">VERIFIED LIVE</span>");
            if (finding.Verified == false) sb.AppendLine("      <span class=\"tag tag--dead\">revoked</span>");
            if (finding.FromHistory)       sb.AppendLine("      <span class=\"tag tag--history\">GIT HISTORY</span>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <span class=\"collapse-arrow\" aria-hidden=\"true\">&#9660;</span>");
            sb.AppendLine("  </div>");

            sb.AppendLine($"  <div class=\"finding__body\" id=\"{findingId}\">");
            sb.AppendLine($"    <p class=\"finding__file\">&#128196;&nbsp;{WebUtility.HtmlEncode(finding.File)}{(finding.Line.HasValue ? $"<span class=\"file-line\">:{finding.Line}</span>" : "")}</p>");
            sb.AppendLine("    <div class=\"finding__detail\">");
            sb.AppendLine($"      <div class=\"detail-block\"><span class=\"detail-label\">Why this matters</span><p class=\"detail-text\">{WebUtility.HtmlEncode(finding.Why)}</p></div>");
            sb.AppendLine($"      <div class=\"detail-block detail-block--fix\"><span class=\"detail-label\">Fix</span><p class=\"detail-text\">{WebUtility.HtmlEncode(finding.Fix)}</p><button class=\"copy-btn\" onclick=\"copyText('{WebUtility.HtmlEncode(finding.Fix.Replace("'", "\\'"))}', this)\">Copy fix</button></div>");
            if (!string.IsNullOrWhiteSpace(finding.RedactedSnippet))
                sb.AppendLine($"      <div class=\"detail-block\"><span class=\"detail-label\">Matched (blinded)</span><code class=\"blinded-snippet\">{WebUtility.HtmlEncode(finding.RedactedSnippet)}</code></div>");
            if (!string.IsNullOrWhiteSpace(finding.Example))
                sb.AppendLine($"      <pre class=\"code-example\">{WebUtility.HtmlEncode(finding.Example)}</pre>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</article>");
        }

        sb.AppendLine("</div>");
    }

    private static void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("<footer class=\"site-footer\">");
        sb.AppendLine("  <div class=\"site-footer__inner\">");

        sb.AppendLine("    <div class=\"footer-brand\">");
        sb.AppendLine($"      <a href=\"{SiteUrl}\" class=\"footer-brand__name\" target=\"_blank\" rel=\"noopener\">REVENANT SYSTEMS</a>");
        sb.AppendLine("      <span class=\"footer-brand__tagline\">Built to outlive.</span>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <div class=\"footer-links\">");
        sb.AppendLine("      <span class=\"footer-links__label\">More projects</span>");
        sb.AppendLine($"      <a href=\"{EchoUrl}\" class=\"footer-link\" target=\"_blank\" rel=\"noopener\">Revenant Echo</a>");
        sb.AppendLine($"      <a href=\"{SortingHatUrl}\" class=\"footer-link\" target=\"_blank\" rel=\"noopener\">Sorting Hat</a>");
        sb.AppendLine($"      <a href=\"{RelayUrl}\" class=\"footer-link\" target=\"_blank\" rel=\"noopener\">Relay</a>");
        sb.AppendLine($"      <a href=\"{GithubOrg}\" class=\"footer-link footer-link--gh\" target=\"_blank\" rel=\"noopener\">GitHub ↗</a>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <p class=\"footer-copy\">Revenant Workspace Sidekick v1.0 &mdash; Revenant Systems LLC</p>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</footer>");
    }

    private static string Css() => """
        <style>
        @font-face {
            font-family: 'GhostTheory';
            src: url('data:font/ttf;base64,AAEAAAALAIAAAwAwT1MvMj/6je8AAAE4AAAAVmNtYXBqK+joAAAEMAAAAtZnYXNw//8AAwAAN5QAAAAIZ2x5ZmuCilIAAAhcAAAooGhlYWT1ODFiAAAAvAAAADZoaGVhDo4GbQAAAPQAAAAkaG10eNCkD3QAAAGQAAACoGxvY2HTuMpiAAAHCAAAAVJtYXhwAK4AWgAAARgAAAAgbmFtZZygYD4AADD8AAAE+nBvc3RyBGQvAAA1+AAAAZoAAQAAAAEAAKP5m/tfDzz1AAsIAAAAAADHh2tyAAAAAMeIfc3/jf0OB30GGgAAAAYAAQAAAAAAAAABAAAHPv5OAEMIRv+N/zAHfQABAAAAAAAAAAAAAAAAAAAAqAABAAAAqABaAAUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEDMwGQAAUACAWaBTMAAAEbBZoFMwAAA9EAZgISAAACAAUAAAAAAAAAgAAAp1AAAEoAAAAAAAAAAEhMICAAQAAg+wIF0/5RATMHPgGyIAABEUEAAAAAAAAAAAAB/AAAAfwAAAKqAKkFOAB1BAAAbAaqAEgGOQBLAqoAVAKqAC4EAP/aBDYARgP2AC0COQAjBAAASgQAAPAEAAAsBAAAUwP2ACsEAABiBAAAWAQAAEwEAAB8BAAAUQP2ACAD9v/5A/YAMAONAFwHXgBhBccAEAVWACIFVgBKBccAIwTjACoEcwAhBccASAXHACMCqgAzAx3/xgXHACIE4wApBx0AIgXH/+UFxwBIBHMAIgXHAEgFVgAjBHMAgATjAD4FxwALBccAEgeNABsFxwAPBccAEwTjABoC9gAAAjkAIgL2AAAD9gAABAAAAAONAEkEAP/7A40ARgQAAEQDjQBMAqoATwQAAD0D3QARAjkAPAI5ABYEAAARAjkAPQY5ABEEAAAMBAAARQQA//4EAwBEAqoADQMdAGQCOQAUBAAAAgQAABEFxwANBAAAGwQAAAwDjQApAtb//AFqAGYDDP+NAqAAAARzAAACiAAAAp4AAAP2AAAD9gAAA/YAAAScAAAD9gAAAs4AAAasAAAHrAAABqwAAALsAAAGBAAABgQAAAYEAAAGBAAABgQAAAYEAAAIRgAABXcAAAR/AAAEfwAABH8AAAR/AAACkQAAApEAAAKRAAACkQAABe4AAAYSAAAGTgAABk4AAAZOAAAGTgAABk4AAAZOAAAF3QAABd0AAAXdAAAF3QAABNEAAARgAAAE4wAAA5oAAAOaAAADmgAAA5oAAAOaAAADmgAABa4AAAOJAAADjQAAA40AAAONAAADjQAAAgIAAAICAAACAgAAAgIAAARzAAAEVAAAA/IAAAPyAAAD8gAAA/IAAAPyAAAD8gAABCMAAAQjAAAEIwAABCMAAANkAAAD8gAAA2QAAAXZAAAEMQAABDkAAAAAAAMAAAADAAAAHAABAAAAAADaAAMAAQAAABwABAC+AAAAKgAgAAQACgAhACYAKwAtADkAXwB9AKIAqgCtAK8AswC1ALoA1gD2AP8hIvAC+wL//wAAACAAIwAoAC0ALwA8AGEAoACqAK0ArwCxALUAuQC8ANgA+CEi8AH7Af///+L/4f/g/9//3v/c/9sAAP+x/1//rf+s/6v/qP+n/6b/pd+DEKUFpQABAAAAAAAAAAAAAAAAAAAAHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAWQBaAAYB/AAAAAAA+QAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgADAAAABAAFAAYABwAAAAgACQAKAAsAAAAMAAAADQAOAA8AEAARABIAEwAUABUAFgAXAAAAAAAYABkAGgAbABwAHQAeAB8AIAAhACIAIwAkACUAJgAnACgAKQAqACsALAAtAC4ALwAwADEAMgAzADQANQA2ADcAOAA5ADoAOwAAADwAPQA+AD8AQABBAEIAQwBEAEUARgBHAEgASQBKAEsATABNAE4ATwBQAFEAUgBTAFQAVQBWAFcAWAAAAAAAawBsAG4AcAB4AH0AggCHAIYAiACKAIkAiwCNAI8AjgCQAJEAkwCSAJQAlQCXAJkAmACaAJwAmwCfAJ4AoAChAAAAAABaAAAAAAAAAAAAhQAAAAAApQAAAAAAAABtAH4AAABdAAAAAAAAAGAAAAAAAAAAAAAAAFsAYgAAAIwAnQBmAFkAAAAAAAAAAAAAAAAAAAAAAAIAZwBqAHwAAAAAAAAAAAAAAAAAAAAAAAAAAACkAAAAAAAAAAAAAACmAKcAAAAAAAAAAAAAAGkAcQBoAHIAbwB0AHUAdgBzAHoAewAAAHkAgACBAH8AAAAAAAAAXAAAAAAAAAAAAAAAIABcALABFAGCAagBzgH4AgwCGgIoAnQCmgLOAxgDQgN6A8oD5AQyBIAElgSqBMAE/AWABbgGDAZIBoAGzgcSB2IHxAf0CBoIfgiyCPwJOgmCCcQKGgpqCrwK7gsyC2oLxgwsDHQMmAyYDKYMpgymDLINCg1EDXoNvg3wDiIOpA7kDxIPOg+QD6wQKBB0ELQQ/BE0EXIRuhHmEiASWBKiEvYTRBNuE9oT5hRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAUUBRQFFAAAAACAKn/ZwISBWsADAAQAAABIwMmNTQ2MzIWFRQHAxcHJwFoJlgGQy8vQQRktLW0AWcDKjUaP0xMSxgr/D+0tbQAAgB1AAAEwwWaAAMAHwAAAQMzEwMzAzMTMwMhByEDIQchAyMTIwMjEyE3IRMhNyECVFXyVqZdjfKOXI0BAx3+/VMBFx7+6Ixci/COXIz+7x4BD1b+2R0BJwNb/t4BIgI//iMB3f4jYf7eYv4oAdj+KAHYYgEiYQAAAwBs/2QDmQW+ACUALAA1AAATMx4BFxEmJyY1NDY3NTMVFhcWFxEjLgEnER4BFRQGBxUjNS4BJwERDgEVFBYTNjc+ATU0JidsLAuWleFLNbeqQFI4HaMnDYuL/I/RukBeomIBYmRhWK1LKDhBT50BTomSDgJIi2tMbXu4E1xcBQ4HPv7Vn5MO/gSwp3yK0hCAgAQjKAMaAdEMZ05Jf/yQDRgjcUBCdmwABQBI/8gGYwVrAAMAEQAiADEAQQAACQEjASEyFhUUBiMiLgE1ND4BFyIGFRQXFhcWMzI3NjU0JyYBMh4BFRQGIyIuATU0PgEXIgcGFRQXFjMyNzY1NCcmBXD8JFkD3PxVh5Wodk+ET1CLRjNPFhEkFR8wIjIxIAOpR41NqnRJiU9PiUcwIy0uIjAuJDAwIQVr+l0Fo+CRrr5XrGlpsVc4eMCLSTceEjRNtL5NM/1uWqxnsbtap2tprlY1NkbDs0czN0myvEszAAADAEv/4QX7BWsAMQA9AEkAAAEhFQ4BAgceATMyNjcXDgEjIiYnDgEjIiY1NDY3LgE1NDc2MzIWFRQGBxYXNjU0JyYnJT4BNTQmIyIGFRQWEy4BJw4BFRQWMzI2BBUBpFdTsG5ZjEdFYBQlJaRtUqlkfMdxpcKu8C8ifmJ9d5aRuH+KsB4WKf6CfHtZQldZIs2EZj14eYp1P3UDaSUHP/7CimhTS0kbjYZZam5VsHp58YFohT2rWkaNZ2qgX+K80pAuJBsFCzuWXEheeToxef0ZtKZ7RaZha6IyAAAAAQBU/koCfAWOABMAAAEVJicmAjUQADcVDgECFRQXHgICfJdlkJwBMvZ7nk4hGkp9/m8lTGaRAYrUATYB/24qROz+lsXWr4qnmgAAAAEALv5KAlYFjgATAAATNRYXFhIVEAAHNT4BEjU0Jy4CLphlj5z+z/d7n00hGUt8BWQqS2aS/nfV/sr+AW4lResBa8XVsIqmmgAAAAAF/9oAowQABOAAAgAFAAgACwAOAAABEyMlBQcJAScJATcJARcB6iZL/hUCPh0CBf3bHAFe/qNB/rUBGEEE4P2h9MpGAQ7+90b98wHzJ/3oAh0mAAACAEYAKAQGA+gAAgAFAAABBScBAzcEBvxDAwG2I0ACPS1A/dgDvgIAAQAtAnID7QKyAAIAAAEFJwPt/EMDAp8tQAAAAAEAIwAmAiIFNgACAAA3ARcjAatUJgUQHQAAAAADAEr/6AO3BWgAEAAkADAAABM0Ajc2MzIXFhEUAgYjIicmNxAXFjMyNjc2ETQnJicmIyIHBgIkFhUUBiMiJjU0NjNKjHRaYJx8m4jTYsKBbcRFOXE2dB4uMCQ5KTpENUg0AS5SUjs6UlI6Ap7oAU9SQZ/F/q/s/raV5cH3/uixlWFyrAE56JtzMCE9U/6cAltAQFtbQEBbAAEA8AAAAwYFaAAWAAATJTMRFB4BFxUhNT4CNRE0Jy4BIyIH8AFKIRM8XP4CYDgWCgclGiVCBMeh+4dyOB4CJSUCHTF6AtyUKiAeHwABACwAAAOrBWgAHgAAAQMhNQgBNTQmIyIGByM+ATMyFhUUBwYHAgchMj4BNwOrX/zgAWEBIJ5uZJ8mJRnPm6XdMEqm+T4BYmxXRhoBBf77JQFCAZipgaZ1cbnG1JBnZ6K1/vA4EDEtAAEAU//oA1YFaAAyAAATPgEzMhcWFRQHHgEVFAcGISImNTQ2MzIXHgEXFjMyNjU0JyYnLgErATU+AjU0JiMiB2g6sYSjV0K6fYBwkv7riWMvIRkaEXgXJSpmlyMaHyuWTiBPn0iBYJtoBEqJlWpPWpSeMbZ7sIGoRCcdLAgFPwYLnmxPSzgdKEEeCl6ET2d/pgAAAAACACsAAAPmBZoAAgAYAAABEQEFFRQ7ARUhNTMyPQEhNQEzETMyNzMHAlj+WAIxSEz+RkxR/dMClCIThTwxZwG2Aq79UmSrdTIyeKgjBCX8HIToAAABAGL/6AN5BUwAIQAAAQchBwQXFhUUDgEHBiMiJjU0NjMyFhcWMzI2NTQmJyYnAQN5Tv5oWQEJm4VXhFFzeXpvLiMaJy9LTXWxnottvAEEBUyqtieeiLhrtoAnN1MyHCsQITSxf3vVOi0HAg8AAAAAAwBY/+gDsQVoABgAKAA0AAABFQ4DBzYzMhYVFAcGIyInJhE0EiQ2MwEGFRQWFxYzMjY1NCYjIgYeARUUBiMiJjU0NjMDloSno2skkJGLzGd8zIthvpIBD/hr/cwSR0YzSVeJiH0mV79SUjs6UlI6BWglDU+ix4lj4LCqjKpcswEdtgFI/lj9RIdTYOFCL6SYq/ogp1tAQFtbQEBbAAAAAAEATP/kA6UFTAALAAATIRUBIwEhIgcGByfOAtf+PHABlf6LcTBUMx0FTCb6vgTFGy5gCwADAHz/6AOKBWgAGQAmADMAAAEuATU0NjMyFhUUBgcWFxYVFAYjIicmNTQ2JT4BNTQmIyIGFRQWFxMOARUUFjMyNjU0JyYBiaFdzKmkyGyrsDlM2rHBbFZ5ATF4QHZmZoA1MTZTUI1tbIImRwKrhKBWhL+yckyea4hOZnGPy3lhc1qx1mx9T2l3dk80aC/+50alYIGbeldIOWoAAAADAFH/5AOoBWgAFwAnADMAABc1PgESNwYjIiY1NDc2MzIXFhUUAgcGIwE2NTQuASMiBhUUFxYzMjYCFhUUBiMiJjU0NjNsguDRKZ1/j8xme8ind5LexqG+AjMSQnlNWYZZQV8ufnZSUjs6UlI6HCUCdQEkr2Xdt7KLqYqr++L+eYFqArmCTmHheKCe03dWLAHDW0BAW1tAQFsAAAACACAA1AOdA8gAAgAFAAAJAScJATcDnfyeGwN5/IgYA8j+ZTr+bQFnOwAAAAL/+QEnA+4DSAACAAUAAAEFJwMlFwPu/EMDNQO8BAM1LUD94T5AAAACADAA8gOuA+YAAgAFAAABBwkBFwEDrhv8nQNmGPyGAoU6AZv+rjv+mQAAAAIAXP9nAzsFawAiACYAAAEjPgE3PgE1NCYjIgYVFBYVFAYjIiY1NDYzMhcWFRQGBw4BBxcHJwHGKQcxTTwkh2JXZDwuISpFwqbOYUhBW5FBDbS1tAFAfqWTcXk+f5ZSMCVsHCQxU0pxrnhYa0maaKSJ6LS1tAAAAAIAYf5GBywFjgBCAFQAAAEDDgEVFBYzMjYSNTQCJCMiBAIVFBIEMyAAEzMCACEiJAI1EBIAMzIEEhUUAgYjIiY1NDcOASMiJjU0Ejc2MzIWFzcHIgcGBwYVFBYzMj4BNzY1NCYFgHVBHCsgSc2Spf7Ttuf+dOfLAXDUAQcBqXQ6Wv4m/tHu/mji+AG/+88BTa6j/IlMTRKUskRHbr2cc1tDWRAhwktNcVA7Ryw6gIwgOEkDvv5x4HgkICySAUiyqwEim/X+NPjm/ojBARoBD/7u/qzhAZ34AQgByQEBq/63ubf+maRIQThbs2p/apEBaHFTRUVuE090xZBYP09Yt1yfZEJOAAAAAAIAEAAABbAFawAcAB8AAAEhBwYVFBYXFSE1Njc2NwEzAR4BFxUhNT4BNTQnCwIDqf3zXCI7Yv5VVRkzPgHdIwHYOV1T/elROShu5uwBxtZPJx8vByUlDxgwkwRc+5iIUQUlJQQuISxfAQ0CJP3cAAADACIAAATmBUwAHgArADgAAAEWFxYVFA4BIyE1MzI3NjURNCcmKwE1ITIXHgEVFAYlHgEzMj4BNTQmIyIHERYzMjY1NC4BIyIGBwOyjUZhgN/l/YAzVSUXHSdNMwJKpGOWnnz9eyVfOZKTTsK6ZFB0cbW+VsKPPlgbArQeQlyFZblVJTYjcgNsfiEsJRgkt3dmoQ8HBz+CTXeoFvtvG6N4T5JUBAUAAAABAEr/4QUPBWsAJAAAARMjLgEjIgYCFRQSFjMyNjcXBgQjICcmNTQSJDMyFxYzMjc2NwTRHx8+5qGH2n127ZiEynkfZv7wu/6vuYq2AT+9k48qEhsUGgsFa/4zz7aJ/tTfuP7ykHGoFLWo+rr8ywFUu0gWExswAAACACMAAAV5BUwAFgAhAAAzNTMyNzY1ETQnJisBNSEgBBIVEAcGIScWMzIAERAAIyIHIzNWJBYcJ00zAigBMAE9wazB/nXbf1boATL+zvBacyU3IXMDbH8gLCWK/r7T/uW+1GIcAUYBFwEZAUQdAAAAAQAqAAAEtAVMADMAAAERITI3NjczESMmJy4BIyERFB4BOwEyNjc2NzMDITUzMjc+ATURNCcmKwE1IRMjLgEnJiMBrAEqdCc0BiUlDg4SUlX+1hAoOOZzaDA+QSh1++swMCsgFxokVDAEFQ8nFTMyKGUFAv3oIy50/ihjHCMo/kFaJxcgLz59/qwlFxBAYwNxgR4oJf7Xa1AVDwAAAQAhAAAEHwVMAC0AAAERMzI2NzMRIy4CKwERFBcWFxY7ARUhNTMyNzY1ETQnJicmKwE1IRMjLgIjAaP3VU8NJSUBJ0VE9w0KICwwMf26MFQmGA0KHysxMAPxDSMaRWVqBQL960tv/jVPSiX+VmchGRIYJSUxIHoDbGchGRIYJf7WX1koAAAAAAEASP/hBaoFawA0AAABEyMmJyYjIAcGFRQSFjMyNjcRNC4BIzUhFSMiBwYVEQ4BIyAnJjU0NzY3NjMyFhcWMzI2NwTpIyM1VHm+/v2HcZbzgEuMQR9BUgINGU4dFHPgif53zJlWZrKVy0p5bzgTExsDBWv+VKBRdc2t78L+wJUmJQGIZj8hJiY0JW3+YT46/L33s6TDaVcYKRUjMwAAAAABACMAAAWdBUwARQAAASERNCcmJyYrATUhFSMiBw4BFREUFxYXFjsBFSE1MzI3NjURIREUFxYXFjsBFSE1MzI3NjURNCcmJyYrATUhFSMiBw4BFQGlAnYNCiArMDACRDAwKyAXDQofLDAw/bwwUyYZ/YoNCiArMDH9uzBUJhgNCh8sMDACRTEwKx8YAtcBhGghGRIYJSUXEEFk/JVnIRkSGCUlMSB6AZ3+Y2chGRIYJSUxIHoDa2ghGRIYJSUXEEFkAAAAAQAzAAACeAVMAB8AACUVITUzMjc2NRE0JyYnJisBNSEVIyIHBhURFBcWFxYzAnj9uzBUJhgNCh8sMDACRTFTJhkNCiArMCUlJTEgegNsZyEZEhglJTEgevyUZyEZEhgAAf/G/tkDEQVMABQAABM1IRUjIgcGFRECJSQDETQnJicmI8wCRTFTJhgU/YsB5h0NCiArMAUnJSUxIHr9af0UAZIBygMmZyEZEhgAAAEAIgAABdgFTABDAAAJAR4BFxUhNTI2NTQmJwERFBcWFxY7ARUhNTMyNzY1ETQnJicmKwE1IRUjIgcOARURNjcANzY1NCYrATUhFQ4CBwYHAmQB9HuuV/17OjMTNf4sDQogKzAu/b4wVCYYDQofLDAwAkIuLywfGBR1ASk+GyoyHwHyLEhoTBa1AvD+D3tZBiUlJxgYJjQBz/5LZyEZEhglJTEgegNsZyIYEhglJRcQQGT+YRNsARBbKB4XIyUlARY/RhS5AAAAAQApAAAEtwVMACAAAAEXAyE1MzI3NjURNCcmKwE1IRUmDgEVERQXHgE7ATI+AQSWIXT75jNWJRUcJ00zAmZsVyAQDDKDY5x+aAF3B/6QJTggdANrfyAsJSUBKkB5/KxTHxUULnUAAAABACIAAAbyBUwAMAAAIQERFBcWOwEVITUzMjc2NRE0Jy4BIzUhCQEhFSMiBwYVERQXFjsBFSE1MzI3NjURAQNG/fQbJVAw/igwViQWFA5LUwGAAewB5AGAL1ckFhwlUC/9wDBXIxb99QR1/HZ9HyolJTQgcgN2WigdJyX72wQlJTQgcvyKfR8qJSU0IHIDivuLAAAAAf/l/+oFqgVMACcAAAMhARE0JyYrATUhFSMiBwYVESMBERQXFjsBFSE1MzI3NjURLgEnJiMbAXADPRwlUC8B2DBWJBYk/IIbJk8w/igvVyQWOz07HTsFTPwHAw59HyolJTQgcvuJBET8vX0fKiUlNCByA69FLBMJAAAAAAMASP/hBXgFawAQABwAKQAAASIHBhEQFxYzMjc2ETQnLgECFhUUBiMiJjU0NjMTIAAREAAhIAAREDc2At22b4yObrW8c4dKOb09UlI7OlJSOhEBCAGD/nr+6/7o/oPcvwUUgqP+sP63somJogE886aAef4sW0BAW1tAQFsCK/5v/tT+y/5oAY4BPAFDzLEAAAIAIgAABCsFTAAfACwAAAERFBcWOwEVITUzMjc2NRE0JyYrATUhMh4BFRQGIyImJx4BMzI2NTQuASMiBwGkHCZNNP27M1YlFBsnTTMB8bbSkNvIMXJBNVIdaJdIhFQzUAJ7/nWAHywlJTgfdANsgB8sJUuyeqbQDkcKCqGAWJdLEwAAAAMASP5vBXkFawAVACYAMgAABR4BFxUmLAEnJicmAjUQACEgABEUAAEiBwYREBcWMzI3NhE0Jy4BAhYVFAYjIiY1NDYzA4Zm7ZeK/sb+52aQVHqHAYoBGAEKAYX+6/56tm+Mjm61vHOHSjm9PVJSOzpSUjoPsKYMIAVls2U6QWEBG8EBMAGS/m3+zfn+iATqgqP+sP63somJogE886aAef4iW0BAW1tAQFsAAAACACMAAAVoBUwAKAA0AAApAQEGIyImJxEUFxY7ARUhNTMyNzY1ETQnJisBNSEyHgEVFAYHAR4BFwEyFjMyNjU0JiMiBwVo/pb+NTMgDR4QHCZMNf27M1YlFRwnTTMB7tjNj6OrARhgim/8PRMcCcLFn4M6YwJ6AgEB/naAHywlJTgfdANsgB8sJT+pdX24Jv57hlgMApQBqIJ/nxMAAAAAAQCA/+EEBQVrADgAAAERIy4CIyIGFRQXFhceAhUUBiMiJy4BIyIGByMRMx4CMzI2NTQmJyYkLgE1NDYzMhcWMzI2NwOrJRJdrFxoiCs+6b6LS++8OzQfwxoZHQclJRpYtWx9kTc6J/6kk0zgrWx5OBcaIQoFa/4rh6Bef1E+M0t9Zm2UUZrfCQU/Hi8B0ZKRYIRaMmYsHsN0jFSS0zUZHy8AAAABAD4AAASwBUwAHwAAARMjJicuASsBERQXFjsBFSE1MzI3NjURIyIHDgEHIxMEoQ8mCxMfZ1S/GyZPL/3BMFYkFqNfKDRKByYQBUz+wlQkOjf79H0fKiUlNCByBAwOE2xcAT4AAQAL/+AFsQVMAC4AAAE1IRUjIgcGFREUDgEjIiYnJjURNCYrATUhFSMiBwYVERQeAjMyPgE1ETQnJiMD0QHgM1ArFVHtzN7mMCBFTTMCSjRUJBkdTI9ohdJNHCdNBSclJUMfcf3azOGhmoJZ9QISfU4lJTUkcv2xT8xySnS12AIlfyAsAAAAAAEAEv/hBa4FTAAfAAABFQYHBgcBIwEmJy4BJzUhFQ4BFRQXCQE2NTQmJyYnNQWuSCU1Kf4nJf4EJxAZST4CKl44LgFZAUAvOkUFDAVMJQ0hMWX7fgSRWhQfIwUlJQkuJDJq/OUDEXQtHTULAQIlAAABABv/4Qd9BUwAOQAAARUiBgcGBwEjCQEjASYnLgEjNSEVIyIGFRQXARMvASYnJicmJyYjNSEVIyIGFRQXCQE2NTQmJyYjNQd9NUIeFCv+hij+y/7NJP5tLQwURTsB9hg1OCwBC+EoIBUaDRMZGRMpAhAkODQtAQQBAiwdFiY9BUwlJjQjhPu7A2P8nQRmfhcmJSUlMCIjfv0HAodyWzImEw0SCAYlJTApM3/9HwLrfDAXKAgOJQAAAQAPAAAFrwVMAD8AAAkBHgEXFSE1Njc+ATU0JyYnAwEOARUUFhcVITU2Nz4BNwkBLgEnNSEVDgEVFBcbAT4BNTQnLgEnNSEVBgcOAQcDRAEjeXVa/bo6HBUbCQcw5v7kLRI2TP4fMyU+cEgBQP71bZhjAnNQOzDQ8SoTDA8uSAHhOSQ2WlIC7/5OtF8FJSUBCwklExcXEUcBXP6UOicVICoDJSUFEBpYWwGUAYefYwMlJQMuHCVH/skBMTYoFRUQFREBJSUDDxdOaQAAAQATAAAFqQVMAC4AAAEhFSMiDgEHAREUFxY7ARUhNTMyNzY1EQEuAScmIzUhFSMiBhUUFwkBNjU0LgEjA9AB2RoaZFI8/rkcJlIs/cAwViQW/oxCL0oUJgJEHi9PPQEbAQo8HTY2BUwlLlZh/f3+rH0fKiUlNCByAUECOGQyIwolJSwsJF7+SwGiXi4cLBkAAQAaAAAEqgVMABEAAAkBITI2NxcDITUBISIOAQcjEwSa/IUCLICJNSFA+7ADZv5ObGEzFSYcBUz7BnCrBv6ZJQTWL1l6AVMAAQAiABYCCwUvAAIAACUBNwIL/hdUFgT7HgAAAAEAAP9gBAD/nAADAAAVIRUhBAD8AGQ8AAIASf/tA4kDrwAyAD0AACUGBwYjIiY1NDc+ATc1NCYjIgcGFRcUBiMiJjU0NjMyFxYXFhURFB4BMzI3NjcVBiMiJicRBgcOARUUFjMyAgeNJDY9X3seKcvsV1M/JSYCLyYlL7Cfek47HBIKFw8QDBU8cGYxOgGXLE9EVjhMhG0RGYJqQzFEeFYkiWYiIiw6LjI0LVaQKR9CK4X+yYM7FAcNPDiWRJMBXTwZLGA5SF8AAv/7/+QDuQWOABYAJAAAATYzMhYVFAcGIyImJxE0LgEjIgcnJTMZAR4BMzI2NTQmIyIHBgE7hZqN0qKLq1ClVg8gGBwqDgETLTNtOVudnWQ1NSgC9rnx0fSVgDo6A7WcSBoQI3D9KP3cMjPIv7C9GxQAAAABAEb/5ANKA68AIQAAAQ4BIyICNTQAMzIWFRQGIyInLgEnJiMiBwYVFBYzMjc2NwNKJdiDnOgBAbSHrjEsOx4RCyMjPmQ9UaGJYk43NAFctcMBBt/YAQ6PTSYvJhV2Hx5KYqGk+0MueQAAAAACAET/5AQFBY4AHwAtAAAlDgEjIiY1NBIzMhc1NC4BIyIHJyUzERQeATMyNxcFIzURLgIjIgcGFRQWMzICx0OASpbg+MN5Tw8gGBorDQERLQ8hFhstC/7wLgY8Yy9YRVuwbFtnRj37xcUBR02pnUgaECNw+92hRxwRI3HJAdhEcDlPaMjK1wAAAgBM/+QDUwOwABQAHQAAEwYXFjMyNjcXDgEjIgI1NBIzMhYVJSEmJy4BIyIG2gFkZIdahS0fFcqYpevxtprG/YcBqAUQGWM2U4MCO8x0dGN4FInhAQHZ6wEHy6o6WCQ4QIEAAAAAT/82A3oFjAAgAAABEQsBESM1MzU0PgEzMhcWFRQGIyIuAScmIyIOAR0BMxUBplVQsrJYtXFpWDo0HhczSh8fJi5AHOwDTP2m/YQCfAJaSDyJvnVELTgeNSFtExMxZ9ZCSAADAD3+RgPbA68AOwBJAFkAAAEuATU0NjMyFzMyFhcWFRQHDgErARYVFAYjIicOARUUFhcWFxYXHgEVFAcGIyInJjU0NzY3NjcuATU0NgEiBhUUFxYzMjY1NCcmAQ4BFRQXFjMyNjU0JyYnJgE1VFrNoINgwisOAwYFAw8rdzjEpURHLB8hMBxwzj1db2qc+8GFSwsRNQdfNCs5ARVKZEQ0UExiRTP++C8wOmS9tKszNJrhAU4pk1mIxEAFBgkXGgoFBkhwgLYUJjkUESAHBAMFCQ1wUnFjklcyNhgYJUIJYx8xHyNeAod2ep5XQnJ6n1pC/IEzWCUwJD5/SDQWFgQGAAAAAAEAEf5GA2kFjgAnAAABFRE+ATMyEhEQBTU2EhEQJiMiBxEUFhcVITU+ATURNTQmIyIHJyUzAU1najWIjv4UqIdHVlJwNFr+QFsyKSAZKwoBHCAEijv+nos3/vH+9/zoOSQoAUoBfAERzYn+T4NGDiUlClF8A1M7TjAQI3MAAAAAAgA8AAACBwV+ABYAGgAAAREUHgEzFSE1Mj4BNRE0Jy4BIyIHJyUDFwcnAXwZMUH+Q0MuGwkHHhocKA4BFBu0tbQDr/0gVjkcJCQaPFUBYZUsIBkPJHABz7S1tAACABb+RgHpBX4AEQAVAAABERQGIz4BNRE0Jy4BIyIHJyUDFwcnAX7IoIBDCQceGhwoDgEUHrS1tAOv/Gbr5DiTkQKMlyshGQ8kcAHPtLW0AAEAEQAABAwFjgA3AAABETc2NzY1NCYnNSEVDgEPARMWFxYXFjMVITU+ATU0JwERFB4BFxUhNTI3Njc2NRE0LgEjIgcnJQFP6UoMCCEmAY5SbUHr62IiMCQZPv5DJhso/ucZLk3+LkYjFQsPDiAaFSoRARAFjvxw1UQSDAwUHQIgIAIuO9n+13shLw4KJCQBFRMXMwFn/tBZOBgBJCQRCxchUQNCn0cbESNwAAABAD3+WgF7BY4ADAAAARELARE0LgEjIgcnJQF7U1MOHxgaKBEBEQWO+0H9iwJ1A0CbRxoQI3AAAAEAEQAABjADrwBXAAABNjc+ATMyFhc+ATMyFhcWFREUFx4BMxUhNTMyNzY3NjURNCcmIyIGDwEXERQeATMVITUyNjc2NRE0JyYjIgcGBxEUHgEzFSE1Mj4BNRE0Jy4BIyIHJyUzAVBkEi1oM1Z8FWeOS0lxIRYNCjY9/jwTOyEXCgQbJ1Y1a0wCAhU6Rv4xTDkLBSEsTzY1Uy0ZMUv+Oz8yGgkHHhocJw8BFCsC7GQPJipkX3hLS1U6fP92ViAWHyQkFxAjEVABinAuQDVICyv+S14uHyQkJCQRUgGKcDFAHSw3/hVaNhskJBs7VQFelywhGQ8kcAAAAQAMAAAD9wOvADMAAAE2MzIWFxYVERQXHgEzFSE1MzI2NzY1ETQmIyIHERQXHgEzFSE1MzI2NRE0LgEjIgcnJTMBS6GSS2wgFg4LMUL+OxNAMwoEQU13dgsOMUv+OxRGMQ8fGhwnDwEUKwLtwktWPHz+eVcfGRwkJCcmD08Bd31xgv4dXRYdGyQkR2QBVKVIGg8kcAAAAAADAEX/5AO5A68ADwAdACkAAAEyFxYVFA4BIyInJjU0PgEXIg4BFRQSMzI2NTQnJgIWFRQGIyImNTQ2MwIA0H5rds9/z3pnfcxTNWtCn4JhfmlHElJSOzpSUjoDr56Hr3v8gKWLrX75d0E/nnzI/t6gw/SMYP78W0BAW1tAQFsAAAL//v0OA7oDrwAcAC4AAAMlMxU+ATMyFxYVFAcGIyInJicRCwERNC4BIyIHBREUFx4BMzI3NjU0JyYjIgcGAgEaJkePT4pccYhwqko2KDJEYhAjHhglATQJDm1TZD5RXEBYMC8kAzly1nlhbITU7Zt/FQ8t/un98AIWA2JZMBgOf/6qbyM6WE5mudJxThgSAAIARP0OA2sDrwASACAAAAERCwERDgEjIiY1NAAzMhYXNjcDETQuASMiBhUUFjMyNgNrUFVbiEmF0QEUwzlgJjo1gydkP3Cgo3M7XAOv+2b9+QIHAYpsT/LL6QElICAcJP0vAa5LVjy+wbnAMwAAAAABAA0AAAK3A68AKAAAARU2MzIWFRQGIyImIyIHBgcRFBceATMVITUyNzY3NjURNC4BIyIHJyUBTHN5N0g0JCNXFRIVLTATDUI+/itGIhkKBQ0jGh8nCgEVA6/OzkMsJzZFFCle/klMJxskJCQWECMRUAFjoD0cDyRwAAAAAQBk/+QC1QOvADEAAAERIy4BIyIGFRQXFh8BFhUUBiMiJyYjIgcjETMeATMyNjU0JiQnJjU0NjMyFxYzMjY3ApAhJndcRlYgH1+Sy711VGwhFRcNISEcnmJFV2H+3i0tm3s2TTMREBIMA6/+yJNqSi04KCkuR2OifZkeChoBR4yOUTlFXpA6OVdxmBcPDhgAAQAU//ECPATBABsAAAERMxUjERQWMzI2NzMOASMiLgE1ESM1PgE3NjcBStbWMyghPhEnI4BELlgqkTdzLRcpBMH+00b9rlk+KShiYzNfYwJoIRZpSCZlAAEAAv/kA/0DlAAlAAABERQeATMyNxcFIzUOASMiLgE1ETQuAQc1IREUFjMyNjcRNCYnNQNjDyEWHycO/u4tdnxFTXEsHDdIAUFZPyttSzlaA5T91Z9HHBEjccKAQlmMgAGZQTIbASX9m4BQNkwCB043AiUAAQAR/+QD7QOUACAAABMhFSMiBhUUFxsBNjU0Jy4BIzUhFQYHBgcBIwEuAScmJxEBrxwnKRXV1hcICyI0ASs0FCMc/rsp/rkWKB8RMgOUJSYgIzD+BgINOB0OCQ8LJSUEER5G/O4DBTYvEAkIAAAAAAEADf/kBbQDlAAsAAATIRUOARUUFxsBJyYnJic1IRUGBwYVFBcbATY1NCYnNSEVBgcBIwMBIwEuAScNAYA1IRHExTQYJxY8AbRIHhQI0MEUJzkBIVcp/s4p5f71Jf7aHTg8A5QlBB4cHyz98QGthzwXDgMlJQMXECMUFf3yAfs2IBMeAiUlDWn86wJJ/bcDAkkzDQABABsAAAPnA5QAOAAAEyEVIgYVFBcWHwE3NjU0JiM1IRUGBwYPARMeARcVITUyNzY1NC8BBwYVFBYXFSE1Njc2PwEnLgEjGwGvKSEjCxZBS0giJgE2MSQxVX3kVEg5/lAtGRNAhpNELS3+1SQbJlrArkpRPQOUJRwXGDIQImhoYxoVHSUlAxgicqf+uHkxAyQkFA4XF13ExFsRGCcCJCQFFB13//xsNwABAAz+RgP0A5QAMgAAEyEVIyIGFRQXGwE2NTQnLgEjNSEVDgEHBgcBDgEjIiY1NDYzMhcWMzI2PwEBJicmJyYnDAGrFS0tId/NEQcIIisBKiUoGAkZ/os2r1E7TDcwITkoCh5HJEH+tw8hGRAXMwOUJScdJ0X+MgH6KSgSCQsNJSUEGCEOP/xuhYhELCozFg8+WZ8Csx8uIwwQDAABACkAAANsA5QAFQAAAQMhNQEhIgYHBgcjNyEVASEyNjc2NwNcC/zYAmD+1GE8ExsEKAYDAP2aAU5pSxcQCwEZ/uckAyoZIzJK/iX81CMsIGcAAAAC//z+0gNJBhoAJgBNAAABFS4BNTQ2NTQmJzU+ATU0JjU0NjcVDgEVFBYVFAYHHgEVFAYVFBYnFS4BNTQ2NTQmJzU+ATU0JjU0NjcVDgEVFBYVFAYHHgEVFAYVFBYDSafRLnlra3ku0ad1bS2Uk5CXLW2Cp9EGeWtreRrRp3VtGZSTkJcFbf71IxfhiUi/NUh9DikOfEk1vkmI4hYjHH9MO8FEZb40NcJlRME7TH9sIxfhiUgzNUh9DikOfEk1PEmI4hYjHH9MOz9EZb40NcJlRDU7TH8AAAABAGb+RwEGBd4AAgAAEzMDZqBpBd74aQAC/43+0gLLBhoAJgBNAAAUNjU0JjU0NjcuATU0NjU0Jic1HgEVFAYVFBYXFQ4BFRQWFRQGBzUkNjU0JjU0NjcuATU0NjU0Jic1HgEVFAYVFBYXFQ4BFRQWFRQGBzVrLJSOkZEsa3OkzS13aWl3Lc2kAWZrBZSNkJEYa3Okzhp3aWl3Bs2k7n9MO8FEZcI1NL5lRME7TH8cIxbiiEm+NUl8DikOfUg1v0iJ4Rcjpn9MOzVEZcI1NL5lRD87TH8cIxbiiEk8NUl8DikOfUg1M0iJ4RcjAAAAKgH+AAEAAAAAAAEADAAHAAEAAAAAAAIABwAAAAEAAAAAAAMAGQAHAAEAAAAAAAQADAAHAAEAAAAAAAUALgAgAAEAAAAAAAYACwBOAAEAAAAAAAcAKgBZAAEAAAAAAAkAAACDAAEAAAAAAAoAPwCDAAMAAQQDAAIADAKyAAMAAQQFAAIAEADCAAMAAQQGAAIADADSAAMAAQQHAAIAEADeAAMAAQQIAAIAEADuAAMAAQQJAAEAGAEMAAMAAQQJAAIADgD+AAMAAQQJAAMAMgEMAAMAAQQJAAQAGAEMAAMAAQQJAAUAXAE+AAMAAQQJAAYAFgGaAAMAAQQJAAcAVAGwAAMAAQQJAAkAAAIEAAMAAQQJAAoAfgIEAAMAAQQKAAIADAKyAAMAAQQLAAIAEAKCAAMAAQQMAAIADAKyAAMAAQQOAAIADALQAAMAAQQQAAIADgKSAAMAAQQTAAIAEgKgAAMAAQQUAAIADAKyAAMAAQQVAAIAEAKyAAMAAQQWAAIADAKyAAMAAQQZAAIADgLCAAMAAQQbAAIAEALQAAMAAQQdAAIADAKyAAMAAQQfAAIADAKyAAMAAQQkAAIADgLgAAMAAQQtAAIADgLuAAMAAQgKAAIADAKyAAMAAQgWAAIADAKyAAMAAQwKAAIADAKyAAMAAQwMAAIADAKyUmVndWxhckdob3N0IFRoZW9yeTpWZXJzaW9uIDEuMDBWZXJzaW9uIDEuMDAgSmFudWFyeSAyOCwgMjAxMCwgaW5pdGlhbCByZWxlYXNlR2hvc3RUaGVvcnk8aG9uZGEgZm9udD6oIFRyYWRlbWFyayBvZiAoeW91ciBjb21wYW55KS5UaGlzIGZvbnQgd2FzIGNyZWF0ZWQgdXNpbmcgRm9udENyZWF0b3IgNS42IGZyb20gSGlnaC1Mb2dpYy5jb20AbwBiAHkBDQBlAGoAbgDpAG4AbwByAG0AYQBsAFMAdABhAG4AZABhAHIAZAOaA7EDvQO/A70DuQO6A6wAUgBlAGcAdQBsAGEAcgBHAGgAbwBzAHQAIABUAGgAZQBvAHIAeQA6AFYAZQByAHMAaQBvAG4AIAAxAC4AMAAwAFYAZQByAHMAaQBvAG4AIAAxAC4AMAAwACAASgBhAG4AdQBhAHIAeQAgADIAOAAsACAAMgAwADEAMAAsACAAaQBuAGkAdABpAGEAbAAgAHIAZQBsAGUAYQBzAGUARwBoAG8AcwB0AFQAaABlAG8AcgB5ADwAaABvAG4AZABhACAAZgBvAG4AdAA+AK4AIABUAHIAYQBkAGUAbQBhAHIAawAgAG8AZgAgACgAeQBvAHUAcgAgAGMAbwBtAHAAYQBuAHkAKQAuAFQAaABpAHMAIABmAG8AbgB0ACAAdwBhAHMAIABjAHIAZQBhAHQAZQBkACAAdQBzAGkAbgBnACAARgBvAG4AdABDAHIAZQBhAHQAbwByACAANQAuADYAIABmAHIAbwBtACAASABpAGcAaAAtAEwAbwBnAGkAYwAuAGMAbwBtAE4AbwByAG0AYQBhAGwAaQBOAG8AcgBtAGEAbABlAFMAdABhAG4AZABhAGEAcgBkAE4AbwByAG0AYQBsAG4AeQQeBDEESwRHBD0ESwQ5AE4AbwByAG0A4QBsAG4AZQBOAGEAdgBhAGQAbgBvAEEAcgByAHUAbgB0AGEAAAACAAAAAAAA/ycAlgAAAAAAAAAAAAAAAAAAAAAAAAAAAKgAAQACAAMABAAGAAcACAAJAAsADAANAA4AEAASABMAFAAVABYAFwAYABkAGgAbABwAHwAgACEAIgAjACQAJQAmACcAKAApACoAKwAsAC0ALgAvADAAMQAyADMANAA1ADYANwA4ADkAOgA7ADwAPQA+AD8AQABBAEIARABFAEYARwBIAEkASgBLAEwATQBOAE8AUABRAFIAUwBUAFUAVgBXAFgAWQBaAFsAXABdAF4AXwBgAKMAhACdANoAkwECAQMAlwEEAJ4A9QD0APYAogCtAMkAxwCuAGIAYwCQAGQAywBlAMgAygDPAMwAzQDOAOkAZgDTANAA0QCvAGcAkQDWANQA1QBoAOsA7QCJAGoAaQBrAG0AbABuAKAAbwBxAHAAcgBzAHUAdAB2AHcA6gB4AHoAeQB7AH0AfAChAH8AfgCAAIEA7ADuALoAjAEFAQYHdW5pMDBCMgd1bmkwMEIzB3VuaTAwQjkHdW5pRjAwMQd1bmlGMDAyAAAAAAAB//8AAg==') format('truetype');
            font-weight: normal;
            font-style: normal;
        }

        :root {
            --bg:            #080b0f;
            --bg-surface:    #0c1118;
            --bg-card:       #0f1520;
            --border:        #1a2535;
            --border-subtle: #111e2c;
            --teal:          #00c896;
            --teal-bright:   #00e8b0;
            --teal-dim:      #009e74;
            --teal-bg:       rgba(0,200,150,.07);
            --text-primary:  #e0e8f0;
            --text-secondary:#7a8fa8;
            --text-muted:    #3d5068;
            --critical:      #ff3c3c;
            --high:          #ff7c2b;
            --medium:        #f5c518;
            --low:           #4ec9b0;
            --info:          #5595cc;
        }

        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        body {
            font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
            background: var(--bg);
            color: var(--text-primary);
            line-height: 1.6;
            min-height: 100vh;
        }

        /* ── Header ── */
        .site-header {
            background: var(--bg-surface);
            border-bottom: 1px solid var(--border-subtle);
            padding: 0.75rem 2rem;
            position: sticky;
            top: 0;
            z-index: 10;
        }
        .site-header__inner {
            max-width: 1040px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 1.5rem;
        }
        .site-header__brand { display: flex; align-items: center; gap: 0.9rem; }
        .site-logo { height: 44px; width: auto; display: block; }
        .site-header__text { display: flex; flex-direction: column; gap: 0.05rem; }
        .brand-name {
            font-family: 'Segoe UI', system-ui, sans-serif;
            font-size: 0.6rem;
            font-weight: 800;
            letter-spacing: 0.3em;
            text-transform: uppercase;
            color: var(--teal);
            text-decoration: none;
        }
        .brand-name:hover { color: var(--teal-bright); }
        .tool-name {
            font-family: 'GhostTheory', 'Segoe UI', system-ui, sans-serif;
            font-size: 1.05rem;
            color: var(--text-primary);
            letter-spacing: 0.04em;
        }
        .site-nav { display: flex; align-items: center; gap: 1.25rem; }
        .nav-link { font-size: 0.8rem; color: var(--text-secondary); text-decoration: none; transition: color .15s; }
        .nav-link:hover { color: var(--teal); }

        /* ── Hero ── */
        .hero {
            background: linear-gradient(180deg, var(--bg-surface) 0%, var(--bg) 100%);
            border-bottom: 1px solid var(--border-subtle);
            padding: 2.5rem 2rem;
        }
        .hero__inner {
            max-width: 1040px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            gap: 3rem;
        }
        .score-ring-wrap { flex-shrink: 0; }
        .score-ring { display: block; }
        .hero__body { flex: 1; min-width: 0; }
        .hero__label {
            font-size: 0.6rem;
            font-weight: 800;
            letter-spacing: 0.3em;
            color: var(--teal-dim);
            margin-bottom: 0.4rem;
        }
        .hero__path {
            font-family: 'Courier New', Consolas, monospace;
            font-size: 0.85rem;
            color: var(--text-secondary);
            word-break: break-all;
            margin-bottom: 1.25rem;
        }
        .sev-chips { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1.25rem; }
        .sev-chip {
            display: inline-flex;
            align-items: center;
            gap: 0.4rem;
            padding: 0.35rem 0.75rem;
            border-radius: 5px;
            font-size: 0.8rem;
            font-weight: 600;
            border: 1px solid transparent;
        }
        .sev-chip__count { font-size: 1rem; font-weight: 900; }
        .sev-chip--critical { background: rgba(255,60,60,.12);  color: var(--critical); border-color: rgba(255,60,60,.3); }
        .sev-chip--high     { background: rgba(255,124,43,.12); color: var(--high);     border-color: rgba(255,124,43,.3); }
        .sev-chip--medium   { background: rgba(245,197,24,.12); color: var(--medium);   border-color: rgba(245,197,24,.3); }
        .sev-chip--low      { background: rgba(78,201,176,.12); color: var(--low);      border-color: rgba(78,201,176,.3); }
        .sev-chip--clean    { background: var(--teal-bg); color: var(--teal);           border-color: rgba(0,200,150,.3); }
        .hero__meta { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
        .meta-item { font-size: 0.82rem; color: var(--text-secondary); }
        .meta-label { color: var(--text-muted); margin-right: 0.25rem; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.08em; }
        .meta-sep { color: var(--border); }

        /* ── Container ── */
        .container { max-width: 1040px; margin: 0 auto; padding: 2rem 2rem 4rem; }

        /* ── Clean state ── */
        .clean-state { text-align: center; padding: 4rem 2rem; color: var(--text-secondary); }
        .clean-state__icon { font-size: 3rem; color: var(--teal); margin-bottom: 1rem; }
        .clean-state__title { font-size: 1.25rem; color: var(--text-primary); margin-bottom: 0.5rem; }
        .clean-state__sub { font-size: 0.9rem; }

        /* ── Toolbar ── */
        .findings-toolbar {
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 0.5rem;
            margin: 0 0 1.5rem;
        }
        .filter-btns { display: flex; align-items: center; flex-wrap: wrap; gap: 0.35rem; }
        .filter-btn {
            padding: 0.3rem 0.7rem;
            border-radius: 4px;
            border: 1px solid var(--border);
            background: var(--bg-card);
            color: var(--text-secondary);
            font-size: 0.78rem;
            cursor: pointer;
            transition: all .15s;
            display: inline-flex;
            align-items: center;
            gap: 0.3rem;
        }
        .filter-btn.active, .filter-btn:hover { border-color: var(--teal); color: var(--teal); }
        .filter-btn--critical.active { border-color: var(--critical); color: var(--critical); }
        .filter-btn--high.active     { border-color: var(--high);     color: var(--high); }
        .filter-btn--medium.active   { border-color: var(--medium);   color: var(--medium); }
        .filter-btn--low.active      { border-color: var(--low);      color: var(--low); }
        .filter-count { font-weight: 700; }
        .search-box {
            margin-left: auto;
            padding: 0.3rem 0.75rem;
            width: 220px;
            border-radius: 4px;
            border: 1px solid var(--border);
            background: var(--bg-card);
            color: var(--text-primary);
            font-size: 0.82rem;
            outline: none;
            transition: border-color .15s;
        }
        .search-box:focus { border-color: var(--teal); }
        .search-box::placeholder { color: var(--text-muted); }

        /* ── Section label ── */
        .section-label {
            font-size: 0.6rem;
            font-weight: 800;
            letter-spacing: 0.3em;
            color: var(--text-muted);
            padding-bottom: 0.75rem;
            border-bottom: 1px solid var(--border-subtle);
            margin-bottom: 1rem;
        }

        /* ── Finding cards ── */
        .finding {
            background: var(--bg-card);
            border: 1px solid var(--border-subtle);
            border-radius: 6px;
            margin-bottom: 0.6rem;
            overflow: hidden;
            transition: border-color .15s;
        }
        .finding:hover { border-color: var(--border); }
        .finding--critical { border-left: 3px solid var(--critical); }
        .finding--high     { border-left: 3px solid var(--high); }
        .finding--medium   { border-left: 3px solid var(--medium); }
        .finding--low      { border-left: 3px solid var(--low); }
        .finding--info     { border-left: 3px solid var(--info); }

        .finding__header {
            display: flex;
            align-items: center;
            gap: 0.6rem;
            padding: 0.85rem 1.25rem;
            cursor: pointer;
            user-select: none;
            flex-wrap: wrap;
        }
        .finding__header:hover { background: rgba(255,255,255,.02); }

        .sev-badge {
            display: inline-block;
            padding: 2px 7px;
            border-radius: 3px;
            font-size: 0.65rem;
            font-weight: 800;
            letter-spacing: 0.07em;
            flex-shrink: 0;
        }
        .sev-badge--critical { background: rgba(255,60,60,.15);  color: var(--critical); border: 1px solid rgba(255,60,60,.35); }
        .sev-badge--high     { background: rgba(255,124,43,.15); color: var(--high);     border: 1px solid rgba(255,124,43,.35); }
        .sev-badge--medium   { background: rgba(245,197,24,.15); color: var(--medium);   border: 1px solid rgba(245,197,24,.35); }
        .sev-badge--low      { background: rgba(78,201,176,.15); color: var(--low);      border: 1px solid rgba(78,201,176,.35); }
        .sev-badge--info     { background: rgba(85,149,204,.15); color: var(--info);     border: 1px solid rgba(85,149,204,.35); }

        .rule-id    { font-family: 'Courier New', Consolas, monospace; font-size: 0.72rem; color: var(--text-muted); flex-shrink: 0; }
        .rule-title { font-size: 0.9rem; color: var(--text-primary); font-weight: 500; }
        .finding__tags { display: flex; gap: 0.3rem; margin-left: auto; }
        .tag { padding: 1px 6px; border-radius: 3px; font-size: 0.62rem; font-weight: 700; letter-spacing: 0.06em; }
        .tag--live    { background: rgba(255,60,60,.2);    color: #ff8a8a; border: 1px solid rgba(255,60,60,.4); }
        .tag--dead    { background: rgba(100,100,100,.2);  color: #888;    border: 1px solid #333; }
        .tag--history { background: rgba(111,63,207,.2);   color: #b89aff; border: 1px solid rgba(111,63,207,.4); }
        .collapse-arrow { color: var(--text-muted); font-size: 0.7rem; transition: transform .2s; flex-shrink: 0; }

        .finding__body {
            border-top: 1px solid var(--border-subtle);
            padding: 1rem 1.25rem;
            overflow: hidden;
            max-height: 2000px;
            transition: max-height .25s ease, padding .25s ease;
        }
        .finding__body.collapsed { max-height: 0; padding-top: 0; padding-bottom: 0; }

        .finding__file {
            font-family: 'Courier New', Consolas, monospace;
            font-size: 0.75rem;
            color: var(--text-muted);
            margin-bottom: 0.85rem;
        }
        .file-line { color: var(--teal-dim); }
        .finding__detail { display: flex; flex-direction: column; gap: 0.75rem; }
        .detail-block { display: flex; flex-direction: column; gap: 0.25rem; }
        .detail-label { font-size: 0.65rem; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase; color: var(--text-muted); }
        .detail-text { font-size: 0.85rem; color: var(--text-secondary); }
        .detail-block--fix { flex-direction: row; align-items: flex-start; flex-wrap: wrap; gap: 0.5rem; }
        .detail-block--fix .detail-label { width: 100%; }
        .detail-block--fix .detail-text { flex: 1; min-width: 200px; }

        .copy-btn {
            padding: 0.2rem 0.6rem;
            border-radius: 3px;
            border: 1px solid var(--border);
            background: transparent;
            color: var(--text-muted);
            font-size: 0.72rem;
            cursor: pointer;
            transition: all .15s;
            flex-shrink: 0;
            align-self: flex-start;
        }
        .copy-btn:hover { border-color: var(--teal); color: var(--teal); }

        .blinded-snippet {
            display: inline-block;
            background: rgba(255,160,0,.08);
            border: 1px solid rgba(255,160,0,.2);
            color: #e8a800;
            font-family: 'Courier New', Consolas, monospace;
            font-size: 0.8rem;
            padding: 0.15rem 0.45rem;
            border-radius: 3px;
        }
        .code-example {
            background: var(--bg-surface);
            border: 1px solid var(--border-subtle);
            border-radius: 4px;
            padding: 0.75rem 1rem;
            font-family: 'Courier New', Consolas, monospace;
            font-size: 0.78rem;
            color: #7ec850;
            overflow-x: auto;
            white-space: pre-wrap;
            margin-top: 0.5rem;
        }

        /* ── Footer ── */
        .site-footer {
            background: var(--bg-surface);
            border-top: 1px solid var(--border-subtle);
            padding: 2rem;
            margin-top: 2rem;
        }
        .site-footer__inner {
            max-width: 1040px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 1.5rem;
            justify-content: space-between;
        }
        .footer-brand { display: flex; flex-direction: column; gap: 0.2rem; }
        .footer-brand__name {
            font-family: 'Segoe UI', system-ui, sans-serif;
            font-size: 0.6rem;
            font-weight: 800;
            letter-spacing: 0.3em;
            color: var(--teal);
            text-decoration: none;
        }
        .footer-brand__name:hover { color: var(--teal-bright); }
        .footer-brand__tagline { font-size: 0.75rem; color: var(--text-muted); font-style: italic; }
        .footer-links { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
        .footer-links__label { font-size: 0.7rem; color: var(--text-muted); letter-spacing: 0.08em; text-transform: uppercase; }
        .footer-link { font-size: 0.8rem; color: var(--text-secondary); text-decoration: none; transition: color .15s; }
        .footer-link:hover { color: var(--teal); }
        .footer-link--gh { color: var(--teal-dim); }
        .footer-copy { font-size: 0.72rem; color: var(--text-muted); }
        </style>
        """;

    private static string InteractiveJs() => """
        <script>
        function filterFindings(sev) {
            document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
            const btn = document.querySelector(`.filter-btn[data-sev="${sev}"]`);
            if (btn) btn.classList.add('active');
            document.querySelectorAll('#findings-list .finding').forEach(el => {
                el.style.display = (sev === 'all' || el.dataset.severity === sev) ? '' : 'none';
            });
        }
        function searchFindings(query) {
            const q = query.trim().toLowerCase();
            document.querySelectorAll('#findings-list .finding').forEach(el => {
                el.style.display = (!q || (el.dataset.searchtext || '').includes(q)) ? '' : 'none';
            });
        }
        function toggleFinding(id) {
            const body = document.getElementById(id);
            if (!body) return;
            body.classList.toggle('collapsed');
            const arrow = body.previousElementSibling?.querySelector('.collapse-arrow');
            if (arrow) arrow.style.transform = body.classList.contains('collapsed') ? 'rotate(-90deg)' : '';
        }
        function copyText(text, btn) {
            navigator.clipboard?.writeText(text).then(() => {
                const orig = btn.textContent;
                btn.textContent = 'Copied!';
                setTimeout(() => btn.textContent = orig, 1500);
            });
        }
        </script>
        """;

    private static string LoadIconBase64()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("rws-icon.png", StringComparison.OrdinalIgnoreCase));
        if (name is null) return string.Empty;
        using var stream = asm.GetManifestResourceStream(name)!;
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }
}
