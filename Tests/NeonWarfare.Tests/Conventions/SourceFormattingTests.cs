using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// The .editorconfig rules for [*.cs] that describe the shape of the text rather than the code in it.
/// The editor only draws the 120-column guide, nothing enforces it — and a line past the guide is
/// invisible in a side-by-side diff, which is where most of this code gets read.
/// The scope is every hand-written .cs of the repository, the test projects included: how wide a line
/// may be is not a property of the game project.
/// </summary>
public class SourceFormattingTests
{
    private const int MaxLineLength = 120;

    [Theory]
    [MemberData(nameof(FileSources.CSharpFiles), MemberType = typeof(FileSources))]
    public void Lines_FitIntoMaxLineLength(string relativePath)
    {
        string[] lines = TextFile.ReadLines(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: lines longer than {MaxLineLength} characters");

        for (int i = 0; i < lines.Length; i++)
        {
            // Length in characters, not bytes: a comment may hold non-ASCII text, and a byte-based
            // check would measure it as longer than it looks.
            if (lines[i].Length > MaxLineLength)
            {
                report.Add($"line {i + 1}: {lines[i].Length} characters");
            }
        }

        report.AssertEmpty();
    }
}
