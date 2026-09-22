using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// The .editorconfig rules for [*.cs] that describe the shape of the text rather than the code in it.
/// The editor only draws the 120-column guide, nothing enforces it — and a line past the guide is
/// invisible in a side-by-side diff, which is where most of this code gets read.
/// The scope is every hand-written .cs of the repository, the test projects included: how wide a line
/// may be is not a property of the game project.
/// </summary>
public class SourceFormattingTests
{
    private const string Section = "*.cs";

    private static readonly int MaxLineLength = EditorConfigFile.Current.IntValue(Section, "max_line_length");

    private static readonly int TabWidth = EditorConfigFile.Current.IntValue(Section, "tab_width");

    [Theory]
    [MemberData(nameof(FileSources.CSharpFiles), MemberType = typeof(FileSources))]
    public void Lines_FitIntoMaxLineLength(string relativePath)
    {
        string[] lines = TextFile.ReadLines(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new(
            $"{relativePath}: lines wider than {MaxLineLength} columns (tab width {TabWidth})");

        for (int i = 0; i < lines.Length; i++)
        {
            int width = Width(lines[i]);
            if (width > MaxLineLength)
            {
                report.Add($"line {i + 1}: {width} columns");
            }
        }

        report.AssertEmpty();
    }

    // Counted in characters, not bytes: a comment may hold non-ASCII text, and a byte-based check would
    // measure it as longer than it looks. A tab moves to the next tab stop, the way the editor draws it.
    private static int Width(string line)
    {
        int column = 0;
        foreach (char character in line)
        {
            column = character == '\t' ? column + TabWidth - column % TabWidth : column + 1;
        }

        return column;
    }
}
