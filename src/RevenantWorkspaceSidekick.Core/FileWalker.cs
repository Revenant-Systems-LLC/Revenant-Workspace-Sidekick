using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

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
        "bin", "obj", ".git", ".vs", ".idea", "packages", "node_modules",
        // Package-manager-generated lockfiles: not developer-authored, and their
        // base64/hex integrity hashes trip the SEC-001 high-entropy secret heuristic.
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "composer.lock",
        "Gemfile.lock", "Cargo.lock", "poetry.lock", "Pipfile.lock"
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

        if (File.Exists(root))
        {
            var ctx = CreateContext(root, root, includeExtensions);
            if (ctx != null) yield return ctx;
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsExcluded(file, root, excludeSegments))
                continue;

            var ctx = CreateContext(file, root, includeExtensions);
            if (ctx != null)
                yield return ctx;
        }
    }

    private static FileContext? CreateContext(string file, string root, HashSet<string> includeExtensions)
    {
        var ext = System.IO.Path.GetExtension(file);
        if (!includeExtensions.Contains(ext))
            return null;

        string content;
        try
        {
            content = BinaryExtensions.Contains(ext)
                ? ExtractBinaryStrings(file)
                : File.ReadAllText(file);
        }
        catch
        {
            return null;
        }

        var relative = root == file ? System.IO.Path.GetFileName(file) : System.IO.Path.GetRelativePath(root, file);
        return new FileContext(file, relative, content);
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
