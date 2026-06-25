using System.Text;
using System.Text.Json;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core.Reporters;

/// <summary>
/// Emits SARIF 2.1.0 — the standard consumed by GitHub Code Scanning, VS Code, and Azure DevOps.
/// Upload the output file with: gh code-scanning upload-results --sarif rws.sarif
/// </summary>
public sealed class SarifReporter : IReporter
{
    public void Report(ScanResult result, TextWriter output)
    {
        // Build a de-duplicated rule catalog from findings
        var ruleMap = result.Findings
            .GroupBy(f => f.RuleId)
            .ToDictionary(g => g.Key, g => g.First());

        using var writer = new Utf8JsonWriter(
            new StreamToUtf8Adapter(output),
            new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("$schema", "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json");
        writer.WriteString("version", "2.1.0");
        writer.WriteStartArray("runs");
        writer.WriteStartObject();

        // --- tool ---
        writer.WriteStartObject("tool");
        writer.WriteStartObject("driver");
        writer.WriteString("name", "RWS");
        writer.WriteString("fullName", "Revenant Workspace Sidekick");
        writer.WriteString("version", "0.68");
        writer.WriteString("informationUri", "https://github.com/savageAZfck/revenant-workspace-sidekick");
        writer.WriteStartArray("rules");
        foreach (var (ruleId, f) in ruleMap)
        {
            writer.WriteStartObject();
            writer.WriteString("id", ruleId);
            writer.WriteString("name", RuleIdToName(ruleId));
            writer.WriteStartObject("shortDescription");
            writer.WriteString("text", f.Title);
            writer.WriteEndObject();
            writer.WriteStartObject("fullDescription");
            writer.WriteString("text", $"{f.Why} Fix: {f.Fix}");
            writer.WriteEndObject();
            writer.WriteStartObject("defaultConfiguration");
            writer.WriteString("level", SeverityToLevel(f.Severity));
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray(); // rules
        writer.WriteEndObject(); // driver
        writer.WriteEndObject(); // tool

        // --- results ---
        writer.WriteStartArray("results");
        foreach (var f in result.Findings)
        {
            writer.WriteStartObject();
            writer.WriteString("ruleId", f.RuleId);
            writer.WriteString("level", SeverityToLevel(f.Severity));
            writer.WriteStartObject("message");
            writer.WriteString("text", f.RedactedSnippet is null
                ? f.Title
                : $"{f.Title} — matched: {f.RedactedSnippet}");
            writer.WriteEndObject();

            writer.WriteStartArray("locations");
            writer.WriteStartObject();
            writer.WriteStartObject("physicalLocation");
            writer.WriteStartObject("artifactLocation");
            writer.WriteString("uri", f.File.Replace('\\', '/'));
            writer.WriteString("uriBaseId", "%SRCROOT%");
            writer.WriteEndObject();
            if (f.Line.HasValue)
            {
                writer.WriteStartObject("region");
                writer.WriteNumber("startLine", f.Line.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndObject(); // physicalLocation
            writer.WriteEndObject();
            writer.WriteEndArray(); // locations

            writer.WriteStartObject("properties");
            writer.WriteString("why", f.Why);
            writer.WriteString("fix", f.Fix);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
        writer.WriteEndArray(); // results

        // --- invocation summary ---
        writer.WriteStartArray("invocations");
        writer.WriteStartObject();
        writer.WriteBoolean("executionSuccessful", true);
        writer.WriteNumber("duration", (int)result.Duration.TotalMilliseconds);
        writer.WriteEndObject();
        writer.WriteEndArray();

        writer.WriteEndObject(); // run
        writer.WriteEndArray(); // runs
        writer.WriteEndObject(); // root
        writer.Flush();
        output.WriteLine();
    }

    private static string SeverityToLevel(Severity s) => s switch
    {
        Severity.Critical => "error",
        Severity.High => "error",
        Severity.Medium => "warning",
        Severity.Low => "note",
        _ => "none"
    };

    private static string RuleIdToName(string ruleId) =>
        ruleId.Replace("-", "").Replace("_", "");

    // Bridges TextWriter → Stream for Utf8JsonWriter
    private sealed class StreamToUtf8Adapter(TextWriter writer) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            writer.Write(Encoding.UTF8.GetString(buffer, offset, count));
        }

        public override void Flush() => writer.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
