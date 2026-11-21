using CodeReview.Api.Services;

namespace CodeReview.Api.Tests;

public class FindingOutputValidatorTests
{
    [Fact]
    public void ValidJsonProducesFinding()
    {
        const string json = """
        {
          "findings": [{
            "category": "Security",
            "subcategory": "SQL Injection",
            "owasp": "A03:2021",
            "cwe": "CWE-89",
            "severity": "critical",
            "confidence": 0.95,
            "file": "src/users.py",
            "lineStart": 42,
            "lineEnd": 45,
            "description": "User input reaches SQL.",
            "impact": "Unauthorized database access.",
            "suggestedFix": "Use parameterized SQL.",
            "correctedCode": "cursor.execute(sql, (userId,))"
          }]
        }
        """;

        var findings = FindingOutputValidator.ParseAndValidate(json, 10);

        var finding = Assert.Single(findings);
        Assert.Equal("Critical", finding.Severity);
        Assert.Equal(0.95, finding.Confidence);
    }

    [Fact]
    public void InvalidEnumAndRangeAreRejected()
    {
        const string json = """
        {
          "findings": [
            {
              "category": "Security",
              "severity": "Catastrophic",
              "file": "src/a.py",
              "lineStart": 0,
              "lineEnd": 5,
              "description": "Invalid",
              "impact": "Invalid",
              "suggestedFix": "Fix"
            }
          ]
        }
        """;

        var findings = FindingOutputValidator.ParseAndValidate(json, 10);

        Assert.Empty(findings);
    }

    [Fact]
    public void DuplicateFindingsAreRemoved()
    {
        const string json = """
        {
          "findings": [
            {
              "category": "Security",
              "subcategory": "SQL Injection",
              "cwe": "CWE-89",
              "severity": "High",
              "confidence": 0.9,
              "file": "src/a.py",
              "lineStart": 10,
              "lineEnd": 12,
              "description": "Same",
              "impact": "Same",
              "suggestedFix": "Parameterize"
            },
            {
              "category": "Security",
              "subcategory": "SQL Injection",
              "cwe": "CWE-89",
              "severity": "High",
              "confidence": 0.8,
              "file": "src/a.py",
              "lineStart": 10,
              "lineEnd": 12,
              "description": "Same",
              "impact": "Same",
              "suggestedFix": "Parameterize"
            }
          ]
        }
        """;

        var findings = FindingOutputValidator.ParseAndValidate(json, 10);

        Assert.Single(findings);
    }

    [Fact]
    public void NonObjectRootIsRejected()
    {
        Assert.Empty(FindingOutputValidator.ParseAndValidate("[]", 10));
    }
}
