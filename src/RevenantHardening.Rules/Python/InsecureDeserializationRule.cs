using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Python;

/// <summary>RSH-PY-003: Insecure Deserialization (pickle, marshal, shelve, yaml.unsafe_load).</summary>
public sealed partial class InsecureDeserializationRule : IRule
{
    [GeneratedRegex(@"\b(pickle|marshal)\.(loads?|load)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex PickleMarshalRegex();

    [GeneratedRegex(@"\bshelve\.open\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex ShelveRegex();

    [GeneratedRegex(@"\byaml\.unsafe_load\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex YamlUnsafeRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-PY-003",
        Title: "Insecure deserialization",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".py"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // Pickle/Marshal
        foreach (Match match in PickleMarshalRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-003",
                Title: "Insecure deserialization with pickle/marshal",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Deserializing untrusted data using pickle or marshal can execute arbitrary code embedded within the payload, leading to full system compromise.",
                Fix: "Avoid pickle or marshal for untrusted data. Use safer data formats like JSON (json.loads) or Protocol Buffers."
            );
        }

        // Shelve
        foreach (Match match in ShelveRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-003",
                Title: "Insecure database file opening with shelve",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "The 'shelve' module is backed by pickle. Opening an untrusted shelve database file can execute arbitrary code during object deserialization.",
                Fix: "Avoid opening database files from untrusted sources with shelve. Use sqlite3 or a secure key-value store."
            );
        }

        // PyYAML unsafe_load
        foreach (Match match in YamlUnsafeRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-PY-003",
                Title: "Insecure YAML loading",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Using 'yaml.unsafe_load()' allows the YAML parser to instantiate arbitrary Python objects, leading to remote code execution.",
                Fix: "Use 'yaml.safe_load()' or 'yaml.load(..., Loader=yaml.SafeLoader)' to safely parse YAML files."
            );
        }
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < content.Length; i++)
            if (content[i] == '\n') line++;
        return line;
    }
}
