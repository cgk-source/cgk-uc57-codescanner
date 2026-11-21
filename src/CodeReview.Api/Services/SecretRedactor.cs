using System.Text.RegularExpressions;

namespace CodeReview.Api.Services;

public static partial class SecretRedactor
{
    public static string Redact(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var redacted = PrivateKeyRegex().Replace(content, "[REDACTED_PRIVATE_KEY]");
        redacted = BearerRegex().Replace(redacted, "Bearer [REDACTED]");
        redacted = CredentialRegex().Replace(redacted, "$1[REDACTED]");
        redacted = ConnectionStringRegex().Replace(redacted, "$1[REDACTED]");
        return redacted;
    }

    [GeneratedRegex(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.IgnoreCase)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"(?i)\bbearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.CultureInvariant)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"(?i)(password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret)\s*[:=]\s*['""]?[^,\s;'""}]+", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialRegex();

    [GeneratedRegex(@"(?i)((?:server|data source|account key|shared access signature)\s*=\s*)[^;]+", RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringRegex();
}
