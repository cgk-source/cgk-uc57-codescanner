using CodeReview.Api.Models;
using CodeReview.Api.Validators;

namespace CodeReview.Api.Tests;

public class AnalysisRequestValidatorTests
{
    [Fact]
    public void ValidDiffRequestPasses()
    {
        var request = CreateRequest("Diff");

        var result = AnalysisRequestValidator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Fact]
    public void MissingSourceFails()
    {
        var request = new AnalysisRequest(null!, null, null);

        var result = AnalysisRequestValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Equal("INVALID_INPUT", result.Error?.Code);
    }

    [Fact]
    public void UnsupportedSourceTypeFails()
    {
        var request = CreateRequest("Unknown");

        var result = AnalysisRequestValidator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UnsupportedLanguageFails()
    {
        var request = CreateRequest("Diff", ["Go"]);

        var result = AnalysisRequestValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("Go", result.Error?.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void FindingLimitOutsideRangeFails(int limit)
    {
        var request = CreateRequest("Diff", null, limit);

        var result = AnalysisRequestValidator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SourceOver500LinesFails()
    {
        var content = string.Join('\n', Enumerable.Range(1, 501).Select(i => $"line{i}"));
        var request = new AnalysisRequest(
            new AnalysisSource("Diff", content),
            null,
            null);

        var result = AnalysisRequestValidator.Validate(request);

        Assert.False(result.IsValid);
    }

    private static AnalysisRequest CreateRequest(
        string sourceType,
        string[]? languages = null,
        int? maxFindings = null)
    {
        return new AnalysisRequest(
            new AnalysisSource(sourceType, "+var value = 1;"),
            languages,
            maxFindings.HasValue
                ? new AnalysisOptions(MaxFindings: maxFindings.Value)
                : null);
    }
}
