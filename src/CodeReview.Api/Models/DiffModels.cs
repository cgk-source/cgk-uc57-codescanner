namespace CodeReview.Api.Models;

public sealed record DiffLine(int SourceLine, char Kind, string Text);

public sealed record DiffBlock(int StartLine, int EndLine, IReadOnlyList<DiffLine> Lines);

public sealed record ParsedDiffFile(string File, string Language, IReadOnlyList<DiffBlock> Blocks);

public sealed record DiffChunk(string File, string Language, int StartLine, int EndLine, string Content);
