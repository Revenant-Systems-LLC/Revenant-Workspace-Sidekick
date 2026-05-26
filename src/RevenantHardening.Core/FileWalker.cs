using RevenantHardening.Core.Models;

namespace RevenantHardening.Core;

public static class FileWalker
{
    private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".xaml", ".resx", ".csproj", ".props", ".targets",
        ".config", ".json", ".xml", ".appxmanifest",
        ".dll", ".exe", ".py", ".java", ".ts", ".tsx", ".js", ".jsx",
        ".cpp", ".hpp", ".c", ".h", ".cc", ".cxx",
        ".dart", ".go", ".kt", ".kts", ".pl", ".pm", ".php", ".rs", ".swift", ".html", ".htm"
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe"
    };

    private const long MaxBinaryBytes = 50 * 1024 * 1024; // 50 MB cap for binary inspection

    private static readonly HashSet<string> DefaultExcludeSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "packages", "node_modules"
    };

    public static IEnumerable<FileContext> Enumerate(
        string root,
        string[]? extraIncludes = null,
        string[]? extraExcludes = null)
    {
        var excludeSegments = new HashSet<string>(DefaultExcludeSegments, StringComparer.OrdinalIgnoreCase);
        if (extraExcludes != null)
            foreach (var e in extraExcludes)
                excludeSegments.Add(e);

        var includeExtensions = new HashSet<string>(DefaultExtensions, StringComparer.OrdinalIgnoreCase);
        if (extraIncludes != null)
            foreach (var i in extraIncludes)
                includeExtensions.Add(i);

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsExcluded(file, root, excludeSegments))
                continue;

            var ext = System.IO.Path.GetExtension(file);
            if (!includeExtensions.Contains(ext))
                continue;

            string content;
            try
            {
                content = BinaryExtensions.Contains(ext)
                    ? ExtractBinaryStrings(file)
                    : File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            var relative = System.IO.Path.GetRelativePath(root, file);
            yield return new FileContext(file, relative, content);
        }
    }

    private static string ExtractBinaryStrings(string filePath, int minLen = 8)
    {
        var info = new FileInfo(filePath);
        if (info.Length > MaxBinaryBytes)
            return string.Empty;

        var data = File.ReadAllBytes(filePath);
        var strings = new System.Text.StringBuilder();
        var run = new System.Text.StringBuilder();

        foreach (byte b in data)
        {
            if (b >= 0x20 && b < 0x7F)
            {
                run.Append((char)b);
            }
            else
            {
                if (run.Length >= minLen)
                {
                    strings.AppendLine(run.ToString());
                }
                run.Clear();
            }
        }
        if (run.Length >= minLen)
            strings.AppendLine(run.ToString());

        return strings.ToString();
    }

    private static bool IsExcluded(string filePath, string root, HashSet<string> excludeSegments)
    {
        var relative = System.IO.Path.GetRelativePath(root, filePath);
        var parts = relative.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
        {
            if (excludeSegments.Contains(part))
                return true;
        }
        return false;
    }
}
