using RevenantWorkspaceSidekick.Core.Models;

namespace RevenantWorkspaceSidekick.Core;

public static class Scorer
{
    public static (int Score, char Grade) Calculate(IReadOnlyList<Finding> findings)
    {
        var deduction = 0;
        foreach (var f in findings)
        {
            deduction += f.Severity switch
            {
                Severity.Critical => 25,
                Severity.High => 15,
                Severity.Medium => 8,
                Severity.Low => 3,
                _ => 0
            };
        }

        var score = Math.Max(0, 100 - deduction);
        var grade = score switch
        {
            >= 90 => 'A',
            >= 80 => 'B',
            >= 70 => 'C',
            >= 60 => 'D',
            _ => 'F'
        };

        return (score, grade);
    }
}
