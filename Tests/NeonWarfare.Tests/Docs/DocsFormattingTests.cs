using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// The .editorconfig rules for markdown — UTF-8, LF, 120 columns — plus a couple of structural rules
/// the documentation already follows everywhere.
/// </summary>
public class DocsFormattingTests
{
    private const int MaxLineLength = 120;

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void File_IsUtf8WithoutBom(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        bool hasBom = document.Bytes.Length >= 3
                      && document.Bytes[0] == 0xEF
                      && document.Bytes[1] == 0xBB
                      && document.Bytes[2] == 0xBF;

        Assert.False(hasBom, $"{relativePath}: UTF-8 BOM found, .editorconfig requires charset = utf-8");
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void File_UsesLineFeedOnly(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: CR found, .editorconfig requires end_of_line = lf");

        for (int i = 0; i < document.Lines.Length; i++)
        {
            if (document.Lines[i].Contains('\r'))
            {
                report.Add($"line {i + 1}");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void File_EndsWithExactlyOneNewline(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));

        Assert.True(document.Text.EndsWith('\n'), $"{relativePath}: file must end with a newline");
        Assert.False(
            document.Text.EndsWith("\n\n", StringComparison.Ordinal),
            $"{relativePath}: file must end with exactly one newline, not a blank line");
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void Lines_HaveNoStrayTrailingWhitespace(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: stray trailing whitespace");

        for (int i = 0; i < document.Lines.Length; i++)
        {
            string line = document.Lines[i].TrimEnd('\r');
            string trailing = line[line.TrimEnd().Length..];

            // Exactly two spaces is a markdown hard line break and is used on purpose, for example
            // in README.md, Docs/Arch/Scene-tree.md and Docs/Arch/Networking.md. Anything else —
            // a lone space, three or more, or a tab — is an accident.
            if (trailing.Length != 0 && trailing != "  ")
            {
                report.Add($"line {i + 1}: {trailing.Length} trailing whitespace character(s)");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void Lines_FitIntoMaxLineLength(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: lines longer than {MaxLineLength} characters");

        for (int i = 0; i < document.Lines.Length; i++)
        {
            // Fenced blocks (ASCII diagrams, command lines) and table rows cannot be wrapped
            // without breaking them, so only prose is measured.
            if (document.IsFenced[i])
            {
                continue;
            }

            string line = document.Lines[i].TrimEnd('\r');
            if (line.TrimStart().StartsWith('|'))
            {
                continue;
            }

            // Length in characters, not bytes: Cyrillic takes two bytes per character in UTF-8 and a
            // byte-based check would flag almost every line in these docs.
            if (line.Length > MaxLineLength)
            {
                report.Add($"line {i + 1}: {line.Length} characters");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void File_HasSingleTopLevelHeadingOnFirstLine(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        IReadOnlyList<MarkdownHeading> topLevel = document.Headings.Where(h => h.Level == 1).ToList();

        Assert.True(
            topLevel.Count == 1,
            $"{relativePath}: expected exactly one H1, found {topLevel.Count} " +
            $"(lines: {string.Join(", ", topLevel.Select(h => h.Line))})");
        Assert.True(
            topLevel[0].Line == 1,
            $"{relativePath}: the H1 must be the first line, found on line {topLevel[0].Line}");
    }
}
