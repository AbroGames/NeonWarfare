using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// How a text file of this repository is written on disk: LF line endings and UTF-8 without a BOM, the
/// .editorconfig rules for [*.cs] and [*.md] applied to every format, because the reasons hold for all
/// of them. A BOM is invisible and anything reading a file as text without expecting one gets a stray
/// character glued to the front of the first line — the .cs.uid sidecars carry one, and a uid read as
/// "﻿uid://…" matches no reference at all. CR is the same kind of trouble one line at a time.
/// The bytes are read directly rather than through a format helper: the check is about the bytes, and
/// every helper decodes them away.
/// </summary>
public class FileEncodingTests
{
    /// <summary>
    /// Off, not deleted. Godot writes the .cs.uid sidecars with a BOM and rewrites them whenever it
    /// reopens the project, so turning this on means either converting the repository and having the
    /// editor undo it, or exempting the sidecars — which is most of what the check would cover. The
    /// decision is open; the check is kept so that turning it on is one attribute away.
    /// </summary>
    [Theory(Skip = "Godot rewrites the .cs.uid sidecars with a BOM — see the comment above.")]
    [MemberData(nameof(FileSources.TextFiles), MemberType = typeof(FileSources))]
    public void File_IsUtf8WithoutBom(string relativePath)
    {
        byte[] bytes = File.ReadAllBytes(RepositoryPaths.Absolute(relativePath));

        Assert.False(
            TextFile.HasBom(bytes),
            $"{relativePath}: UTF-8 BOM found, .editorconfig requires charset = utf-8");
    }

    [Theory]
    [MemberData(nameof(FileSources.TextFiles), MemberType = typeof(FileSources))]
    public void File_UsesLineFeedOnly(string relativePath)
    {
        string[] lines = File.ReadAllText(RepositoryPaths.Absolute(relativePath)).Split('\n');
        FailureReport report = new($"{relativePath}: CR found, .editorconfig requires end_of_line = lf");

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains('\r'))
            {
                report.Add($"line {i + 1}");
            }
        }

        report.AssertEmpty();
    }
}
