using RevenantWorkspaceSidekick.Core;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests;

public class FileWalkerTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rws-filewalker-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Theory]
    [InlineData("package-lock.json")]
    [InlineData("yarn.lock")]
    [InlineData("pnpm-lock.yaml")]
    [InlineData("composer.lock")]
    [InlineData("Gemfile.lock")]
    [InlineData("Cargo.lock")]
    [InlineData("poetry.lock")]
    [InlineData("Pipfile.lock")]
    public void Enumerate_ExcludesPackageManagerLockfiles(string lockfileName)
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, lockfileName), "{ \"integrity\": \"sha512-abc\" }");
            File.WriteAllText(Path.Combine(dir, "app.json"), "{ \"key\": \"value\" }");

            var files = FileWalker.Enumerate(dir).ToList();

            Assert.DoesNotContain(files, f => f.RelativePath == lockfileName);
            Assert.Contains(files, f => f.RelativePath == "app.json");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
