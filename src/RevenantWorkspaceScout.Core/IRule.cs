using RevenantWorkspaceScout.Core.Models;

namespace RevenantWorkspaceScout.Core;

public interface IRule
{
    RuleMetadata Metadata { get; }
    IEnumerable<Finding> Analyze(FileContext context);
}
