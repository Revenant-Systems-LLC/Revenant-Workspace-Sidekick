using System.Text.Json;
using System.Text.Json.Serialization;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core.Reporters;

public sealed class JsonReporter : IReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void Report(ScanResult result, TextWriter output)
    {
        var dto = new
        {
            scanRoot = result.ScanRoot,
            filesScanned = result.FilesScanned,
            durationSeconds = result.Duration.TotalSeconds,
            score = result.Score,
            grade = result.Grade.ToString(),
            findingCount = result.Findings.Count,
            findings = result.Findings.Select(f => new
            {
                ruleId = f.RuleId,
                title = f.Title,
                severity = f.Severity.ToString().ToLowerInvariant(),
                file = f.File,
                line = f.Line,
                why = f.Why,
                fix = f.Fix,
                redactedSnippet = f.RedactedSnippet
            })
        };

        output.Write(JsonSerializer.Serialize(dto, Options));
    }
}
