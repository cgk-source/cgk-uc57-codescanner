using CodeReview.Api.Models;
using CodeReview.Api.Reports;

namespace CodeReview.Api.Tests;

public class HtmlReportRendererTests
{
    [Fact]
    public void EscapesFindingContentAndAddsSeverityBadge()
    {
        var finding = new Finding(
            "FND-001",
            "Security",
            "XSS",
            null,
            "CWE-79",
            "High",
            0.9,
            "src/<script>.tsx",
            10,
            12,
            "<script>alert('x')</script>",
            "Injected markup could execute.",
            "Escape output.",
            "<div>{value}</div>");
        var analysis = new AnalysisResponse(
            Guid.NewGuid(),
            "Completed",
            new AnalysisSource("Diff", "+text"),
            new FindingSummary(0, 1, 0, 0, 0, 1),
            [finding],
            new AnalysisMetadata(["JavaScript"], 100, "test", true));

        var html = HtmlReportRenderer.Render(analysis);

        Assert.Contains("High", html);
        Assert.Contains("src/&lt;script&gt;.tsx", html);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&lt;div&gt;", html);
    }
}
