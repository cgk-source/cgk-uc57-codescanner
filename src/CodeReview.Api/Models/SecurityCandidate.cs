namespace CodeReview.Api.Models;

public sealed record SecurityCandidate(
    string Rule,
    string Category,
    string Severity,
    string? Cwe,
    string? Owasp,
    int Line,
    string Evidence,
    string Rationale);
