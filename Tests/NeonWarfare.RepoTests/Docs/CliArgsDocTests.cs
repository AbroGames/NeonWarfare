using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// Docs/Cli-args.md lists every flag the game understands, and that list is the only description there
/// is: the flags are plain strings, so neither adding one nor deleting one makes the build say anything
/// about the document.
/// </summary>
public class CliArgsDocTests
{
    /// <summary>A flag mentioned anywhere in the document, prose included.</summary>
    private static readonly Regex MentionedFlagRegex =
        new(@"`(?<flag>--[a-z0-9-]+)", RegexOptions.Compiled);

    private const string DocumentName = "Cli-args.md";

    private const string FlagPrefix = "--";

    private const int FlagColumn = 0;

    private static readonly string[] FlagTableColumns = ["Flag", "Description"];

    /// <summary>
    /// Godot's own arguments. <c>--path</c> points at the project folder and has nothing to do with the
    /// game's flags; the document says so in a paragraph of its own rather than in a table row.
    /// </summary>
    private static readonly string[] NotOurFlags = ["--path"];

    [Fact]
    public void CmdArgFlags_AreDocumented()
    {
        IReadOnlySet<string> documented = DocumentedFlags();
        FailureReport report = new(
            $"Flags declared in {RepositoryPaths.Relative(RepositoryPaths.CmdArgsDirectory)} but " +
            $"missing from Docs/{DocumentName}");

        foreach ((CSharpFile file, LiteralExpressionSyntax literal) in DeclaredFlags())
        {
            if (!documented.Contains(literal.Token.ValueText))
            {
                report.Add($"{file.Describe(literal)}: '{literal.Token.ValueText}' — add a table row for it");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedFlags_ExistInCmdArgs()
    {
        FailureReport report = new($"Flags described in Docs/{DocumentName} that the code does not declare");

        CrossCheck.ReportMissing(
            report,
            DocumentedFlags().Order(StringComparer.Ordinal),
            DeclaredFlags().Select(declaration => declaration.Literal.Token.ValueText)
                .ToHashSet(StringComparer.Ordinal),
            NotOurFlags,
            flag => $"'{flag}' — either it was renamed in " +
                    $"{RepositoryPaths.Relative(RepositoryPaths.CmdArgsDirectory)}/, or the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void NotOurFlags_AreMentionedInTheDocument()
    {
        CrossCheck.AssertExemptionsExist(
            nameof(NotOurFlags),
            NotOurFlags,
            MentionedFlags().Contains,
            $"Docs/{DocumentName} no longer mentions it");
    }

    /// <summary>
    /// The first column of the flag tables, placeholder cut off: <c>`--auto-start-savefile &lt;name&gt;`</c>
    /// documents <c>--auto-start-savefile</c>. A flag named only in prose is not documented — the
    /// paragraph about <c>--auto-start</c> versus <c>--auto-connect</c> would otherwise hide a lost row.
    /// </summary>
    private static IReadOnlySet<string> DocumentedFlags() =>
        MarkdownDocument.LoadDoc(DocumentName).Tables
            .Select(RequireFlagColumns)
            .SelectMany(table => table.CodeSpanColumn(FlagColumn))
            .Select(span => span.Split(' ')[0])
            .ToHashSet(StringComparer.Ordinal);

    private static MarkdownTable RequireFlagColumns(MarkdownTable table) =>
        table.Header.SequenceEqual(FlagTableColumns, StringComparer.Ordinal)
            ? table
            : throw new InvalidOperationException(
                $"{table.RelativePath}:{table.Line}: every table there is a flag table with the columns " +
                $"{string.Join(" | ", FlagTableColumns)}, this one has {string.Join(" | ", table.Header)}.");

    /// <summary>
    /// Read from the raw text rather than through MarkdownDocument: that class blanks out code spans in
    /// prose on purpose, and here the flags are exactly what is inside them.
    /// </summary>
    private static IReadOnlySet<string> MentionedFlags() =>
        MentionedFlagRegex.Matches(File.ReadAllText(RepositoryPaths.Doc(DocumentName)))
            .Select(match => match.Groups["flag"].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<(CSharpFile File, LiteralExpressionSyntax Literal)> DeclaredFlags() =>
        RepositoryPaths.CmdArgsFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<LiteralExpressionSyntax>()
                .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                .Where(literal => literal.Token.ValueText.StartsWith(FlagPrefix, StringComparison.Ordinal))
                .Select(literal => (File: file, Literal: literal)));
}
