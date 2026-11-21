using CodeReview.Api.Services;

namespace CodeReview.Api.Tests;

public class StaticSecurityScannerTests
{
    [Fact]
    public void FindsSqlConcatenation()
    {
        const string content = """var query = "SELECT * FROM Users WHERE Id = " + id;""";

        var candidates = StaticSecurityScanner.Scan(content, "C#");

        Assert.Contains(candidates, candidate => candidate.Rule == "SQL_CONCATENATION");
    }

    [Fact]
    public void DoesNotFlagParameterizedQuery()
    {
        const string content = """cursor.execute("SELECT * FROM users WHERE id = %s", (user_id,))""";

        var candidates = StaticSecurityScanner.Scan(content, "Python");

        Assert.DoesNotContain(candidates, candidate => candidate.Rule == "SQL_CONCATENATION");
    }

    [Fact]
    public void FindsDynamicShellAndEval()
    {
        const string content = """
var command = "rm " + fileName;
Process.Start(command);
eval(userInput);
""";

        var candidates = StaticSecurityScanner.Scan(content, "JavaScript");

        Assert.Contains(candidates, candidate => candidate.Rule == "UNSAFE_EVAL");
    }

    [Fact]
    public void FindsWeakCryptoAndDisabledTls()
    {
        const string content = """
MD5.Create();
ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
""";

        var candidates = StaticSecurityScanner.Scan(content, "C#");

        Assert.Contains(candidates, candidate => candidate.Rule == "WEAK_CRYPTO");
        Assert.Contains(candidates, candidate => candidate.Rule == "DISABLED_TLS");
    }
}
