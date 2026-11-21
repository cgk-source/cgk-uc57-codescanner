using System.Text.Json;
using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public static class FindingOutputValidator
{
    private static readonly HashSet<string> Severities =
        new(StringComparer.OrdinalIgnoreCase) { "Critical", "High", "Medium", "Low", "Info" };

    public static IReadOnlyList<Finding> ParseAndValidate(string json, int maxFindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("findings", out var findingsElement) ||
            findingsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var findings = new List<Finding>();

        foreach (var item in findingsElement.EnumerateArray())
        {
            if (findings.Count >= Math.Clamp(maxFindings, 1, 10))
            {
                break;
            }

            if (!TryParse(item, out var finding))
            {
                continue;
            }

            findings.Add(finding);
        }

        return findings
            .GroupBy(finding => $"{finding.File}:{finding.LineStart}:{finding.Category}:{finding.Cwe}")
            .Select(group => group.First())
            .ToList();
    }

    private static bool TryParse(JsonElement item, out Finding finding)
    {
        finding = null!;
        if (item.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryGetRequiredString(item, "category", out var category) ||
            !TryGetRequiredString(item, "severity", out var severity) ||
            !TryGetRequiredString(item, "description", out var description) ||
            !TryGetRequiredString(item, "impact", out var impact) ||
            !TryGetRequiredString(item, "suggestedFix", out var suggestedFix) ||
            !TryGetRequiredString(item, "file", out var file))
        {
            return false;
        }

        if (!Severities.Contains(severity) ||
            !TryGetRequiredInt(item, "lineStart", out var lineStart) ||
            !TryGetRequiredInt(item, "lineEnd", out var lineEnd) ||
            lineStart < 1 ||
            lineEnd < lineStart)
        {
            return false;
        }

        var subcategory = GetOptionalString(item, "subcategory") ?? category;
        var owasp = GetOptionalString(item, "owasp");
        var cwe = GetOptionalString(item, "cwe");
        var correctedCode = GetOptionalString(item, "correctedCode");

        var confidence = item.TryGetProperty("confidence", out var confidenceElement) &&
                         confidenceElement.ValueKind == JsonValueKind.Number &&
                         confidenceElement.TryGetDouble(out var parsedConfidence)
            ? Math.Clamp(parsedConfidence, 0d, 1d)
            : 0.5d;

        finding = new Finding(
            $"FND-{Guid.NewGuid():N}"[..12],
            category,
            subcategory,
            owasp,
            cwe,
            NormalizeSeverity(severity),
            confidence,
            file,
            lineStart,
            lineEnd,
            description,
            impact,
            suggestedFix,
            correctedCode);

        return true;
    }

    private static bool TryGetRequiredString(JsonElement item, string property, out string value)
    {
        value = string.Empty;
        if (!item.TryGetProperty(property, out var element) ||
            element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetRequiredInt(JsonElement item, string property, out int value)
    {
        value = 0;
        return item.TryGetProperty(property, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out value);
    }

    private static string? GetOptionalString(JsonElement item, string property)
    {
        return item.TryGetProperty(property, out var element) &&
               element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static string NormalizeSeverity(string severity)
    {
        return Severities.First(value =>
            value.Equals(severity, StringComparison.OrdinalIgnoreCase));
    }
}
