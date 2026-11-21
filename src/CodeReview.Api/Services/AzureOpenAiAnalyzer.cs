using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CodeReview.Api.Models;
using Microsoft.Extensions.Options;

namespace CodeReview.Api.Services;

public sealed class AzureOpenAiAnalyzer : IAiCodeAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAiOptions _options;

    public AzureOpenAiAnalyzer(HttpClient httpClient, IOptions<AzureOpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AnalysisResponse> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedContent = SecretRedactor.Redact(request.Source.Content);
        var parsedFiles = UnifiedDiffParser.Parse(sanitizedContent);
        var candidates = parsedFiles
            .SelectMany(file => StaticSecurityScanner.Scan(
                string.Join('\n', file.Blocks.SelectMany(block => block.Lines.Select(line => line.Text))),
                file.Language))
            .Take(30)
            .ToList();

        var languages = request.Languages is { Length: > 0 }
            ? request.Languages
            : parsedFiles.Select(file => file.Language).Distinct().ToArray();
        if (languages.Length == 0)
        {
            languages = ["Unknown"];
        }

        var maxFindings = request.Options?.MaxFindings is > 0 and <= 10
            ? request.Options.MaxFindings
            : 10;

        var findings = await AnalyzeWithRetryAsync(
            sanitizedContent,
            candidates,
            maxFindings,
            cancellationToken);

        findings = FindingRanker.Rank(findings, maxFindings);
        stopwatch.Stop();

        var summary = new FindingSummary(
            findings.Count(finding => finding.Severity == "Critical"),
            findings.Count(finding => finding.Severity == "High"),
            findings.Count(finding => finding.Severity == "Medium"),
            findings.Count(finding => finding.Severity == "Low"),
            findings.Count(finding => finding.Severity == "Info"),
            findings.Count);

        return new AnalysisResponse(
            Guid.NewGuid(),
            "Completed",
            request.Source,
            summary,
            findings,
            new AnalysisMetadata(
                languages,
                stopwatch.ElapsedMilliseconds,
                $"azure-openai:{_options.Deployment}",
                true));
    }

    private async Task<IReadOnlyList<Finding>> AnalyzeWithRetryAsync(
        string content,
        IReadOnlyList<SecurityCandidate> candidates,
        int maxFindings,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var responseText = await SendRequestAsync(
                content,
                candidates,
                maxFindings,
                attempt > 0,
                cancellationToken);

            var normalized = ExtractJsonObject(responseText);
            if (normalized is not null)
            {
                try
                {
                    var findings = FindingOutputValidator.ParseAndValidate(normalized, maxFindings);
                    if (findings.Count > 0 || attempt == 1)
                    {
                        return findings;
                    }
                }
                catch (JsonException)
                {
                    // Retry once below.
                }
            }
        }

        return [];
    }

    private async Task<string> SendRequestAsync(
        string content,
        IReadOnlyList<SecurityCandidate> candidates,
        int maxFindings,
        bool stricterRetry,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var candidateJson = JsonSerializer.Serialize(candidates);
        var systemPrompt =
            "You are a senior application-security engineer. Analyze only changed code and nearby context. " +
            "Find security vulnerabilities, OWASP Top 10 issues, logic bugs, reliability defects, and anti-patterns. " +
            "Do not invent findings unsupported by code. Return only a JSON object with a findings array. " +
            $"Return at most {maxFindings} findings. Every finding must include category, subcategory, severity, confidence, " +
            "file, lineStart, lineEnd, description, impact, suggestedFix, and correctedCode. " +
            "Severity must be Critical, High, Medium, Low, or Info. Confidence must be from 0 to 1.";

        if (stricterRetry)
        {
            systemPrompt += " Re-read the source and omit any finding that lacks direct evidence.";
        }

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content =
                        "Static candidate evidence:\n" + candidateJson +
                        "\n\nChanged code diff:\n" + content +
                        "\n\nReturn valid JSON only."
                }
            },
            temperature = 0,
            max_tokens = 6000,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(_options.Deployment)}/chat/completions?api-version={Uri.EscapeDataString(_options.ApiVersion)}");
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(timeout.Token);
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }

    private static string? ExtractJsonObject(string responseText)
    {
        var text = responseText.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = text.IndexOf('{');
            var lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                text = text[firstBrace..(lastBrace + 1)];
            }
        }

        return text.StartsWith("{", StringComparison.Ordinal) ? text : null;
    }
}
