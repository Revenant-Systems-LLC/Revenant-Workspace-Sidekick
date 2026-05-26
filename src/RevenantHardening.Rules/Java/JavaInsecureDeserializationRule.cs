using System.Text.RegularExpressions;
using RevenantHardening.Core;
using RevenantHardening.Core.Models;

namespace RevenantHardening.Rules.Java;

/// <summary>RSH-JV-002: Insecure Java Deserialization.</summary>
public sealed partial class JavaInsecureDeserializationRule : IRule
{
    [GeneratedRegex(@"\bnew\s+ObjectInputStream\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex ObjectInputStreamRegex();

    [GeneratedRegex(@"\bnew\s+XMLDecoder\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex XmlDecoderRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RSH-JV-002",
        Title: "Insecure Java deserialization",
        DefaultSeverity: Severity.Critical,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // ObjectInputStream
        foreach (Match match in ObjectInputStreamRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-JV-002",
                Title: "Insecure object deserialization via ObjectInputStream",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "Deserializing untrusted serialized Java objects using ObjectInputStream allows attackers to achieve remote code execution (RCE) by crafting malicious payloads (e.g. gadget chains).",
                Fix: "Avoid Java native serialization for untrusted inputs. Use safer standard data formats like JSON (Jackson, Gson) or Protocol Buffers, or implement secure lookup-based filtering (e.g., ObjectInputFilter)."
            );
        }

        // XMLDecoder
        foreach (Match match in XmlDecoderRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RSH-JV-002",
                Title: "Insecure XML deserialization via XMLDecoder",
                Severity: Severity.Critical,
                File: context.RelativePath,
                Line: line,
                Why: "XMLDecoder deserialization allows arbitrary method execution based on the structure of the input XML document. This provides a direct path to Remote Code Execution (RCE).",
                Fix: "Never use XMLDecoder to parse XML from untrusted sources. Use a standard safe XML parsing library like Jackson-dataformat-xml, and ensure DTD/external entity loading is disabled."
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
