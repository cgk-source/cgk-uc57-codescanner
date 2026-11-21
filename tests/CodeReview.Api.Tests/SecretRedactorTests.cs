using CodeReview.Api.Services;

namespace CodeReview.Api.Tests;

public class SecretRedactorTests
{
    [Fact]
    public void RedactsApiKeyAndBearerToken()
    {
        const string content = "Authorization: Bearer abc123; api_key=sk-live-secret";

        var result = SecretRedactor.Redact(content);

        Assert.DoesNotContain("abc123", result);
        Assert.DoesNotContain("sk-live-secret", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void RedactsPrivateKey()
    {
        const string content = """
-----BEGIN PRIVATE KEY-----
super-secret-private-material
-----END PRIVATE KEY-----
""";

        var result = SecretRedactor.Redact(content);

        Assert.DoesNotContain("super-secret-private-material", result);
        Assert.Contains("REDACTED_PRIVATE_KEY", result);
    }

    [Fact]
    public void LeavesOrdinaryCodeUnchanged()
    {
        const string content = "var total = quantity * price;";

        Assert.Equal(content, SecretRedactor.Redact(content));
    }
}
