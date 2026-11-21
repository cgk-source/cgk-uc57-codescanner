using System.Diagnostics;
using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public sealed class LocalDemoAnalyzer : IAiCodeAnalyzer
{
    public Task<AnalysisResponse> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedContent = SecretRedactor.Redact(request.Source.Content);
        var language = DetectLanguages(request);
        var findings = BuildDemoFindings(sanitizedContent);
        var findingsLimit = request.Options?.MaxFindings is > 0 and <= 10
            ? request.Options.MaxFindings
            : 10;
        findings = findings.Take(findingsLimit).ToList();

        var summary = new FindingSummary(
            findings.Count(f => f.Severity == "Critical"),
            findings.Count(f => f.Severity == "High"),
            findings.Count(f => f.Severity == "Medium"),
            findings.Count(f => f.Severity == "Low"),
            findings.Count(f => f.Severity == "Info"),
            findings.Count);

        stopwatch.Stop();
        var response = new AnalysisResponse(
            Guid.NewGuid(),
            "Completed",
            request.Source,
            summary,
            findings,
            new AnalysisMetadata(language, stopwatch.ElapsedMilliseconds, "local-demo", true));

        return Task.FromResult(response);
    }

    private static string[] DetectLanguages(AnalysisRequest request)
    {
        if (request.Languages is { Length: > 0 })
        {
            return request.Languages;
        }

        var content = request.Source.Content;
        var languages = new List<string>();
        if (content.Contains("public class ", StringComparison.Ordinal) || content.Contains("package ", StringComparison.Ordinal))
        {
            languages.Add(content.Contains("package ", StringComparison.Ordinal) ? "Java" : "C#");
        }
        if (content.Contains("def ", StringComparison.Ordinal) || content.Contains("import ", StringComparison.Ordinal))
        {
            languages.Add("Python");
        }
        if (content.Contains("function ", StringComparison.Ordinal) || content.Contains("=>", StringComparison.Ordinal))
        {
            languages.Add("JavaScript");
        }
        return languages.Count == 0 ? ["Unknown"] : languages.Distinct().ToArray();
    }

    private static List<Finding> BuildDemoFindings(string content)
    {
        var findings = new List<Finding>();

        if (content.Contains("+", StringComparison.Ordinal) &&
            (content.Contains("execute", StringComparison.OrdinalIgnoreCase) ||
             content.Contains("FromSqlRaw", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new Finding(
                "FND-001", "Security", "SQL Injection", "A03:2021", "CWE-89", "Critical", 0.96,
                "src/Data/UserRepository.cs", 48, 52,
                "User-controlled input may be interpolated directly into a SQL query.",
                "An attacker may read or modify unauthorized database records.",
                "Use a parameterized query or an ORM with bound parameters.",
                "return await db.Users.FromSqlInterpolated($\"SELECT * FROM Users WHERE Id = {id}\").ToListAsync();"));
        }

        if (content.Contains(".Result", StringComparison.Ordinal) ||
            content.Contains(".Wait(", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "FND-002", "AntiPattern", "Blocking Async Calls", null, null, "Medium", 0.78,
                "src/Services/Example.cs", 20, 24,
                "A synchronous call blocks an asynchronous operation.",
                "This can cause deadlocks or thread-pool starvation.",
                "Await the asynchronous operation and propagate cancellation.",
                "var result = await operation.ExecuteAsync(cancellationToken);"));
        }

        if (findings.Count == 0)
        {
            findings.Add(new Finding(
                "FND-001", "Maintainability", "Review Required", null, null, "Info", 0.40,
                "unknown", 1, 1,
                "No deterministic demo rule matched this diff.",
                "The local demo analyzer is not a replacement for the Azure OpenAI provider.",
                "Connect Azure OpenAI and rerun the analysis.",
                null));
        }

        return findings;
    }
}
