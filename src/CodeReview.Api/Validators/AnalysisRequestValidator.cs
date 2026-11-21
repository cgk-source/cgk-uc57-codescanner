using CodeReview.Api.Models;

namespace CodeReview.Api.Validators;

public sealed record ValidationResult(bool IsValid, ApiError? Error)
{
    public static ValidationResult Valid() => new(true, null);
    public static ValidationResult Invalid(string code, string message) => new(false, new ApiError(code, message));
}

public static class AnalysisRequestValidator
{
    private static readonly HashSet<string> SupportedSourceTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Diff", "UploadedFile", "GitHubPullRequest", "AzureDevOpsPullRequest" };

    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase) { "Python", "JavaScript", "TypeScript", "Java", "C#" };

    public static ValidationResult Validate(AnalysisRequest request)
    {
        if (request.Source is null)
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Source is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Source.Type))
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Source type is required.");
        }

        if (!SupportedSourceTypes.Contains(request.Source.Type))
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Unsupported source type.");
        }

        if (string.IsNullOrWhiteSpace(request.Source.Content))
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Source content is required.");
        }

        if (request.Source.Content.Length > 1_000_000)
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Source content exceeds the 1 MB limit.");
        }

        var lineCount = request.Source.Content.Split('\n').Length;
        if (lineCount > 500)
        {
            return ValidationResult.Invalid("INVALID_INPUT", "The source exceeds the 500-line limit.");
        }

        if (request.Languages is not null)
        {
            foreach (var language in request.Languages)
            {
                if (!SupportedLanguages.Contains(language))
                {
                    return ValidationResult.Invalid("INVALID_INPUT", $"Unsupported language: {language}");
                }
            }
        }

        var maxFindings = request.Options?.MaxFindings ?? 10;
        if (maxFindings is < 1 or > 10)
        {
            return ValidationResult.Invalid("INVALID_INPUT", "Max findings must be between 1 and 10.");
        }

        return ValidationResult.Valid();
    }
}
