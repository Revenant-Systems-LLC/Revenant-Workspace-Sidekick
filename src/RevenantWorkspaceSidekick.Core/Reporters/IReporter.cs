using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core.Reporters;

public interface IReporter
{
    void Report(ScanResult result, TextWriter output);
}
