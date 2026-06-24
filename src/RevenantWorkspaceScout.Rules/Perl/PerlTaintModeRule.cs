using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Perl;

/// <summary>RWS-PL-003: Missing taint mode (-T) in Perl shebang.</summary>
public sealed partial class PerlTaintModeRule : IRule
{
    [GeneratedRegex(@"^#!.*\bperl\b.*$", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex PerlShebangRegex();

    [GeneratedRegex(@"\s-\w*T\w*\b")]
    private static partial Regex TaintFlagRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-PL-003",
        Title: "Missing taint mode in shebang",
        DefaultSeverity: Severity.Medium,
        FileExtensions: [".pl"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        var shebangMatch = PerlShebangRegex().Match(context.Content);
        if (!shebangMatch.Success)
            yield break;

        // Shebang with perl exists — check if -T is present
        if (!TaintFlagRegex().IsMatch(shebangMatch.Value))
        {
            yield return new Finding(
                RuleId: "RWS-PL-003",
                Title: "Perl script missing taint mode (-T)",
                Severity: Severity.Medium,
                File: context.RelativePath,
                Line: 1,
                Why: "Taint mode (-T) causes Perl to track data from external sources and prevents it from being used in dangerous operations without explicit validation. Without it, user-supplied data can flow unchecked into system calls.",
                Fix: "Add -T to the shebang line: #!/usr/bin/perl -T"
            );
        }
    }
}
