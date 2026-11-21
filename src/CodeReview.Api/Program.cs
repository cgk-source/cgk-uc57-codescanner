using System.Text.Json.Serialization;
using CodeReview.Api.Models;
using CodeReview.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors();
builder.Services.AddSingleton<IAiCodeAnalyzer, LocalDemoAnalyzer>();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapPost("/api/v1/analyses", async (
    AnalysisRequest request,
    IAiCodeAnalyzer analyzer,
    CancellationToken cancellationToken) =>
{
    if (request.Source is null || string.IsNullOrWhiteSpace(request.Source.Content))
    {
        return Results.BadRequest(new ApiError("INVALID_INPUT", "Source content is required."));
    }

    if (request.Source.Content.Length > 1_000_000)
    {
        return Results.BadRequest(new ApiError("INVALID_INPUT", "Uploaded content exceeds the 1 MB limit."));
    }

    var changedLines = request.Source.Content.Count(character => character == '\n') + 1;
    if (changedLines > 500)
    {
        return Results.BadRequest(new ApiError("INVALID_INPUT", "The diff exceeds the 500-line limit."));
    }

    var result = await analyzer.AnalyzeAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
