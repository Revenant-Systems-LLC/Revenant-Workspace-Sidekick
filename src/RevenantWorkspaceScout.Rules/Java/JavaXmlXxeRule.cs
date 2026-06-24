using System.Text.RegularExpressions;
using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Rules.Java;

/// <summary>RWS-JV-005: Java XML External Entity (XXE) Injection.</summary>
public sealed partial class JavaXmlXxeRule : IRule
{
    [GeneratedRegex(@"\bDocumentBuilderFactory\.newInstance\s*\(\)", RegexOptions.IgnoreCase)]
    private static partial Regex DocBuilderRegex();

    [GeneratedRegex(@"\bSAXParserFactory\.newInstance\s*\(\)", RegexOptions.IgnoreCase)]
    private static partial Regex SaxParserRegex();

    [GeneratedRegex(@"\bXMLInputFactory\.newInstance\s*\(\)", RegexOptions.IgnoreCase)]
    private static partial Regex XmlInputRegex();

    public RuleMetadata Metadata { get; } = new(
        Id: "RWS-JV-005",
        Title: "XML External Entity (XXE) vulnerability",
        DefaultSeverity: Severity.High,
        FileExtensions: [".java"]
    );

    public IEnumerable<Finding> Analyze(FileContext context)
    {
        // Check if there is secure configuration in the same file
        bool isSecured = context.Content.Contains("disallow-doctype-decl") || 
                         context.Content.Contains("external-general-entities") ||
                         context.Content.Contains("SUPPORT_DTD");

        if (isSecured)
            yield break;

        // DocumentBuilderFactory
        foreach (Match match in DocBuilderRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-005",
                Title: "Potentially vulnerable DocumentBuilderFactory instantiation (XXE)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "XML parser configurations that do not explicitly disable external DTDs or entities are vulnerable to XML External Entity (XXE) attacks, allowing file disclosure, SSRF, or Denial of Service.",
                Fix: "Configure the factory to disallow DTD declarations: factory.setFeature(\"http://apache.org/xml/features/disallow-doctype-decl\", true);"
            );
        }

        // SAXParserFactory
        foreach (Match match in SaxParserRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-005",
                Title: "Potentially vulnerable SAXParserFactory instantiation (XXE)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "SAX XML parsers that allow external entities are vulnerable to XXE injection.",
                Fix: "Disable external DOCTYPES: factory.setFeature(\"http://apache.org/xml/features/disallow-doctype-decl\", true);"
            );
        }

        // XMLInputFactory
        foreach (Match match in XmlInputRegex().Matches(context.Content))
        {
            var line = GetLineNumber(context.Content, match.Index);
            yield return new Finding(
                RuleId: "RWS-JV-005",
                Title: "Potentially vulnerable XMLInputFactory instantiation (XXE)",
                Severity: Severity.High,
                File: context.RelativePath,
                Line: line,
                Why: "StAX XML input factories are vulnerable to XXE if DTD support is not explicitly disabled.",
                Fix: "Set properties: factory.setProperty(XMLInputFactory.SUPPORT_DTD, false); and factory.setProperty(XMLInputFactory.IS_SUPPORTING_EXTERNAL_ENTITIES, false);"
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
