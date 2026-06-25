using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

public interface IRule
{
    RuleMetadata Metadata { get; }
    IEnumerable<Finding> Analyze(FileContext context);
}
