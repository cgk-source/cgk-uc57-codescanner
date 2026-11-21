using System.Text.Json.Serialization;
using CodeReview.Api.Models;
using CodeReview.Api.Services;
using CodeReview.Api.Validators;
using CodeReview.Api.Reports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors();
builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));

var azureOptions = builder.Configuration
    .GetSection(AzureOpenAiOptions.SectionName)
    .Get<AzureOpenAiOptions>();

if (!string.IsNullOrWhiteSpace(azureOptions?.Endpoint) &&
    !string.IsNullOrWhiteSpace(azureOptions.ApiKey))
{
    builder.Services.AddHttpClient<IAiCodeAnalyzer, AzureOpenAiAnalyzer>();
}
else
{
    builder.Services.AddSingleton<IAiCodeAnalyzer, LocalDemoAnalyzer>();
}

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapPost("/api/v1/analyses", async (
    AnalysisRequest request,
    IAiCodeAnalyzer analyzer,
    CancellationToken cancellationToken) =>
{
    var validation = AnalysisRequestValidator.Validate(request);
    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var result = await analyzer.AnalyzeAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/v1/reports/html", (AnalysisResponse analysis) =>
{
    var html = HtmlReportRenderer.Render(analysis);
    return Results.Text(html, "text/html; charset=utf-8");
});

app.Run();
