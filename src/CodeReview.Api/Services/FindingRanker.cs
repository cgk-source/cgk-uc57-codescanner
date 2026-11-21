using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public static class FindingRanker
{
    private static readonly IReadOnlyDictionary<string, int> SeverityRank =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Critical"] = 0,
            ["High"] = 1,
            ["Medium"] = 2,
            ["Low"] = 3,
            ["Info"] = 4
        };

    public static IReadOnlyList<Finding> Rank(
        IReadOnlyList<Finding> findings,
        int maxFindings)
    {
        return findings
            .DistinctBy(finding => $"{finding.File}:{finding.LineStart}:{finding.Category}:{finding.Cwe}")
            .OrderBy(finding => SeverityRank.GetValueOrDefault(finding.Severity, 5))
            .ThenByDescending(finding => finding.Confidence)
            .ThenBy(finding => finding.File, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.LineStart)
            .Take(Math.Clamp(maxFindings, 1, 10))
            .ToList();
    }
}
