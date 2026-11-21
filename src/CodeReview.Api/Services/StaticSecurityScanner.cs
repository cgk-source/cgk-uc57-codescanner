using System.Text.RegularExpressions;
using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public static partial class StaticSecurityScanner
{
    public static IReadOnlyList<SecurityCandidate> Scan(string content, string language)
    {
        ArgumentNullException.ThrowIfNull(content);
        var candidates = new List<SecurityCandidate>();

        AddMatches(candidates, content,
            SqlConcatenationRegex(),
            "SQL_CONCATENATION",
            "SQL Injection",
            "Critical",
            "CWE-89",
            "A03:2021 - Injection",
            "SQL text is assembled with string concatenation or interpolation.");

        AddMatches(candidates, content,
            DynamicShellRegex(),
            "DYNAMIC_SHELL",
            "OS Command Injection",
            "Critical",
            "CWE-78",
            "A03:2021 - Injection",
            "A shell or process receives dynamically constructed input.");

        AddMatches(candidates, content,
            UnsafeEvalRegex(),
            "UNSAFE_EVAL",
            "Code Injection",
            "Critical",
            "CWE-95",
            "A03:2021 - Injection",
            "Dynamic code evaluation can execute untrusted input.");

        AddMatches(candidates, content,
            HardcodedSecretRegex(),
            "HARDCODED_SECRET",
            "Hard-coded Secret",
            "High",
            "CWE-798",
            "A07:2021 - Identification and Authentication Failures",
            "A credential appears to be embedded directly in source code.");

        AddMatches(candidates, content,
            WeakCryptoRegex(),
            "WEAK_CRYPTO",
            "Weak Cryptography",
            "High",
            "CWE-327",
            "A02:2021 - Cryptographic Failures",
            "The code uses a weak or obsolete cryptographic primitive.");

        AddMatches(candidates, content,
            UnsafeDeserializationRegex(),
            "UNSAFE_DESERIALIZATION",
            "Unsafe Deserialization",
            "High",
            "CWE-502",
            "A08:2021 - Software and Data Integrity Failures",
            "The code deserializes data through a risky API.");

        AddMatches(candidates, content,
            DisabledTlsRegex(),
            "DISABLED_TLS",
            "Insecure TLS Configuration",
            "High",
            "CWE-295",
            "A02:2021 - Cryptographic Failures",
            "Certificate or TLS validation appears to be disabled.");

        AddMatches(candidates, content,
            PathTraversalRegex(),
            "PATH_TRAVERSAL",
            "Path Traversal",
            "High",
            "CWE-22",
            "A01:2021 - Broken Access Control",
            "A file path appears to be constructed from input without validation.");

        return candidates
            .GroupBy(candidate => $"{candidate.Rule}:{candidate.Line}")
            .Select(group => group.First())
            .ToList();
    }

    private static void AddMatches(
        List<SecurityCandidate> candidates,
        string content,
        Regex regex,
        string rule,
        string category,
        string severity,
        string? cwe,
        string? owasp,
        string rationale)
    {
        foreach (Match match in regex.Matches(content))
        {
            var line = content[..match.Index].Count(character => character == '\n') + 1;
            var evidence = match.Value.Length <= 180
                ? match.Value
                : match.Value[..180];

            candidates.Add(new SecurityCandidate(
                rule,
                category,
                severity,
                cwe,
                owasp,
                line,
                evidence,
                rationale));
        }
    }

    [GeneratedRegex(@"(?:SELECT|INSERT|UPDATE|DELETE)[^;\n]*(?:\+\s*\w+|\$\{[^}]+\}|""\s*\+\s*"")", RegexOptions.IgnoreCase)]
    private static partial Regex SqlConcatenationRegex();

    [GeneratedRegex(@"(?:Runtime\.getRuntime\(\)\.exec|ProcessBuilder|os\.system|subprocess\.(?:run|Popen|call)|child_process|execSync)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex DynamicShellRegex();

    [GeneratedRegex(@"(?:^|[^\w])(?:eval|setTimeout|setInterval)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UnsafeEvalRegex();

    [GeneratedRegex(@"(?i)(?:password|api[_-]?key|access[_-]?token|client[_-]?secret)\s*[:=]\s*[\""'][^\""'\r\n]{8,}[\""']", RegexOptions.CultureInvariant)]
    private static partial Regex HardcodedSecretRegex();

    [GeneratedRegex(@"(?:MD5|SHA1|DES|RC4|ECB)\s*(?:\(|\.|\b)", RegexOptions.IgnoreCase)]
    private static partial Regex WeakCryptoRegex();

    [GeneratedRegex(@"(?:BinaryFormatter|TypeConfusedObjectSerializer|ObjectInputStream|pickle\.loads|yaml\.load)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafeDeserializationRegex();

    [GeneratedRegex(@"(?:ServerCertificate(?:Custom)?ValidationCallback|validate_certificate\s*=?\s*False|NODE_TLS_REJECT_UNAUTHORIZED\s*=\s*['""]?0|TrustAllCerts|InsecureSkipVerify)", RegexOptions.IgnoreCase)]
    private static partial Regex DisabledTlsRegex();

    [GeneratedRegex(@"(?:Path\.Combine|open|readFile|sendFile|fromPath)\s*\([^;\n]*(?:\b(?:name|file|path|id)\b|\+\s*\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex PathTraversalRegex();
}
