using System.Text.Json;
using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

/// <summary>
/// Baseline = a snapshot of known/accepted findings.
/// Re-running with a baseline reports only NEW findings (detect-secrets style).
/// Generate a baseline with: rws baseline update [path]
/// </summary>
public sealed class BaselineManager
{
    private const string FileName = ".rws-baseline.json";

    public static string BaselinePath(string scanRoot) =>
        Path.Combine(scanRoot, FileName);

    public static BaselineFile? Load(string scanRoot)
    {
        var path = BaselinePath(scanRoot);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BaselineFile>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string scanRoot, IEnumerable<Finding> findings)
    {
        var entries = findings.Select(f => new BaselineEntry(f.RuleId, f.File, f.Line, f.Title)).ToList();
        var file = new BaselineFile(DateTime.UtcNow.ToString("yyyy-MM-dd"), "1", entries);
        var json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(BaselinePath(scanRoot), json);
    }

    /// <summary>Returns only findings NOT present in the baseline.</summary>
    public static IReadOnlyList<Finding> FilterNew(IEnumerable<Finding> findings, BaselineFile? baseline)
    {
        if (baseline is null) return findings.ToList();
        var known = baseline.Entries.ToHashSet();
        return findings.Where(f => !known.Contains(new BaselineEntry(f.RuleId, f.File, f.Line, f.Title))).ToList();
    }
}

public sealed record BaselineFile(string CreatedAt, string Version, List<BaselineEntry> Entries);

public sealed record BaselineEntry(string RuleId, string File, int? Line, string Title);
