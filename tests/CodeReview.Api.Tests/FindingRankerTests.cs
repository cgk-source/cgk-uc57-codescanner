using CodeReview.Api.Models;
using CodeReview.Api.Services;

namespace CodeReview.Api.Tests;

public class FindingRankerTests
{
    [Fact]
    public void CriticalFindingsRankBeforeLowAndCapAtTen()
    {
        var findings = Enumerable.Range(1, 12)
            .Select(index => new Finding(
                $"FND-{index}",
                "Security",
                "Test",
                null,
                null,
                index <= 2 ? "Critical" : "Low",
                0.8,
                "src/a.cs",
                index,
                index,
                "Description",
                "Impact",
                "Fix",
                null))
            .ToList();

        var ranked = FindingRanker.Rank(findings, 10);

        Assert.Equal(10, ranked.Count);
        Assert.Equal("Critical", ranked[0].Severity);
        Assert.Equal("Critical", ranked[1].Severity);
    }
}
