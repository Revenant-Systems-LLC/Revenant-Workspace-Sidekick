using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Core.Reporters;

public interface IReporter
{
    void Report(ScanResult result, TextWriter output);
}
