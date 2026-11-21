using CodeReview.Api.Services;

namespace CodeReview.Api.Tests;

public class UnifiedDiffParserTests
{
    [Fact]
    public void ParsesPythonAddedLinesAndDestinationRange()
    {
        const string diff = """
diff --git a/app.py b/app.py
--- a/app.py
+++ b/app.py
@@ -10,2 +10,3 @@
 print("same")
-old
+new
+print("added")
""";

        var files = UnifiedDiffParser.Parse(diff);

        Assert.Single(files);
        Assert.Equal("app.py", files[0].File);
        Assert.Equal("Python", files[0].Language);
        Assert.Equal(10, files[0].Blocks[0].Lines.Min(line => line.SourceLine));
        Assert.Contains(files[0].Blocks[0].Lines, line => line.Text == "new");
        Assert.Contains(files[0].Blocks[0].Lines, line => line.Text == "print(\"added\")");
    }

    [Theory]
    [InlineData("src/App.js", "JavaScript")]
    [InlineData("src/App.jsx", "JavaScript")]
    [InlineData("src/App.ts", "TypeScript")]
    [InlineData("src/App.tsx", "TypeScript")]
    [InlineData("src/App.java", "Java")]
    [InlineData("src/App.cs", "C#")]
    [InlineData("src/readme.md", "Unknown")]
    public void DetectsSupportedLanguages(string file, string expected)
    {
        Assert.Equal(expected, UnifiedDiffParser.DetectLanguage(file));
    }

    [Fact]
    public void ChunksLargeBlockAtConfiguredLimit()
    {
        var lines = Enumerable.Range(1, 301)
            .Select(index => new CodeReview.Api.Models.DiffLine(index, '+', $"line{index}"))
            .ToList();
        var block = new CodeReview.Api.Models.DiffBlock(1, 301, lines);
        var file = new CodeReview.Api.Models.ParsedDiffFile("app.py", "Python", [block]);

        var chunks = UnifiedDiffParser.Chunk([file], 150);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(150, chunks[0].EndLine - chunks[0].StartLine + 1);
        Assert.Contains("line301", chunks[2].Content);
    }

    [Fact]
    public void ParsesMultipleChangedFiles()
    {
        const string diff = """
diff --git a/a.py b/a.py
+++ b/a.py
@@ -1 +1 @@
-a
+b
diff --git a/B.java b/B.java
+++ b/B.java
@@ -5 +5 @@
-old
+new
""";

        var files = UnifiedDiffParser.Parse(diff);

        Assert.Equal(2, files.Count);
        Assert.Equal("a.py", files[0].File);
        Assert.Equal("B.java", files[1].File);
    }
}
