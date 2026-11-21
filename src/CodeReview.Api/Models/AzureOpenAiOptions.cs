namespace CodeReview.Api.Models;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Deployment { get; set; } = "gpt-4o";
    public string ApiVersion { get; set; } = "2024-10-21";
    public int TimeoutSeconds { get; set; } = 15;
}
