namespace CodeReview.Api.Models;

public sealed record AnalysisRequest(AnalysisSource Source, string[]? Languages, AnalysisOptions? Options);

public sealed record AnalysisSource(string Type, string Content, string? Identifier = null);

public sealed record AnalysisOptions(
    bool IncludeQuality = true,
    bool IncludeOWASP = true,
    bool IncludeLogic = true,
    int MaxFindings = 10);

public sealed record ApiError(string Code, string Message);

public sealed record AnalysisResponse(
    Guid AnalysisId,
    string Status,
    AnalysisSource Source,
    FindingSummary Summary,
    IReadOnlyList<Finding> Findings,
    AnalysisMetadata Metadata);

public sealed record FindingSummary(int Critical, int High, int Medium, int Low, int Info, int Total);

public sealed record Finding(
    string Id,
    string Category,
    string Subcategory,
    string? Owasp,
    string? Cwe,
    string Severity,
    double Confidence,
    string File,
    int LineStart,
    int LineEnd,
    string Description,
    string Impact,
    string SuggestedFix,
    string? CorrectedCode);

public sealed record AnalysisMetadata(
    string[] Languages,
    long DurationMs,
    string Model,
    bool SchemaValidated);
