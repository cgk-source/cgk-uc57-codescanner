using System.Text.RegularExpressions;
using CodeReview.Api.Models;

namespace CodeReview.Api.Services;

public static partial class UnifiedDiffParser
{
    public static IReadOnlyList<ParsedDiffFile> Parse(string diff)
    {
        ArgumentException.ThrowIfNullOrEmpty(diff);

        var result = new List<ParsedDiffFile>();
        ParsedDiffFileBuilder? currentFile = null;
        var currentLine = 1;

        foreach (var rawLine in diff.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (rawLine.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                if (currentFile is not null)
                {
                    result.Add(currentFile.Build());
                }
                currentFile = null;
                continue;
            }

            if (rawLine.StartsWith("+++ ", StringComparison.Ordinal))
            {
                var file = rawLine[4..].Trim();
                if (file.StartsWith("b/", StringComparison.Ordinal))
                {
                    file = file[2..];
                }

                if (file != "/dev/null")
                {
                    currentFile = new ParsedDiffFileBuilder(file, DetectLanguage(file));
                }
                continue;
            }

            if (rawLine.StartsWith("@@ ", StringComparison.Ordinal))
            {
                var match = HunkHeaderRegex().Match(rawLine);
                if (match.Success)
                {
                    currentLine = int.Parse(match.Groups["start"].Value);
                    currentFile?.StartBlock(currentLine);
                }
                continue;
            }

            if (currentFile is null)
            {
                continue;
            }

            if (rawLine.StartsWith('+') && !rawLine.StartsWith("+++ ", StringComparison.Ordinal))
            {
                currentFile.AddLine(new DiffLine(currentLine, '+', rawLine[1..]));
                currentLine++;
            }
            else if (rawLine.StartsWith('-'))
            {
                // Removed lines do not advance the destination line.
            }
            else if (rawLine.StartsWith(' '))
            {
                currentFile.AddLine(new DiffLine(currentLine, ' ', rawLine[1..]));
                currentLine++;
            }
        }

        if (currentFile is not null)
        {
            result.Add(currentFile.Build());
        }

        return result;
    }

    public static IReadOnlyList<DiffChunk> Chunk(IReadOnlyList<ParsedDiffFile> files, int maxLines = 150)
    {
        var chunks = new List<DiffChunk>();

        foreach (var file in files)
        {
            foreach (var block in file.Blocks)
            {
                for (var offset = 0; offset < block.Lines.Count; offset += maxLines)
                {
                    var lines = block.Lines.Skip(offset).Take(maxLines).ToList();
                    if (lines.Count == 0)
                    {
                        continue;
                    }

                    chunks.Add(new DiffChunk(
                        file.File,
                        file.Language,
                        lines.Min(line => line.SourceLine),
                        lines.Max(line => line.SourceLine),
                        string.Join('\n', lines.Select(line => $"{line.Kind}{line.Text}"))));
                }
            }
        }

        return chunks;
    }

    public static string DetectLanguage(string file)
    {
        var extension = Path.GetExtension(file).ToLowerInvariant();
        return extension switch
        {
            ".py" => "Python",
            ".js" or ".jsx" or ".mjs" or ".cjs" => "JavaScript",
            ".ts" or ".tsx" => "TypeScript",
            ".java" => "Java",
            ".cs" => "C#",
            _ => "Unknown"
        };
    }

    [GeneratedRegex(@"^\@\@ .* \+(?<start>\d+)(?:,\d+)? \@\@", RegexOptions.CultureInvariant)]
    private static partial Regex HunkHeaderRegex();

    private sealed class ParsedDiffFileBuilder
    {
        private readonly string _file;
        private readonly string _language;
        private readonly List<DiffBlock> _blocks = [];
        private List<DiffLine>? _blockLines;
        private int _blockStart;
        private int _blockEnd;

        public ParsedDiffFileBuilder(string file, string language)
        {
            _file = file;
            _language = language;
        }

        public void StartBlock(int startLine)
        {
            if (_blockLines is { Count: > 0 })
            {
                _blocks.Add(new DiffBlock(_blockStart, _blockEnd, _blockLines));
            }

            _blockLines = [];
            _blockStart = startLine;
            _blockEnd = startLine;
        }

        public void AddLine(DiffLine line)
        {
            _blockLines ??= [];
            _blockLines.Add(line);
            _blockEnd = line.SourceLine;
        }

        public ParsedDiffFile Build()
        {
            if (_blockLines is { Count: > 0 })
            {
                _blocks.Add(new DiffBlock(_blockStart, _blockEnd, _blockLines));
            }

            return new ParsedDiffFile(_file, _language, _blocks);
        }
    }
}
