using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Docs/Cli-args.md lists every flag the game understands, and that list is the only description there
/// is: the flags are plain strings, so neither adding one nor deleting one makes the build say anything
/// about the document.
/// </summary>
public class CliArgsDocTests
{
    /// <summary>A flag as the document writes it — inside a code span, possibly followed by a
    /// placeholder: <c>`--auto-start-savefile &lt;name&gt;`</c>.</summary>
    private static readonly Regex DocumentedFlagRegex =
        new(@"`(?<flag>--[a-z0-9-]+)", RegexOptions.Compiled);

    private const string FlagPrefix = "--";

    /// <summary>
    /// Godot's own arguments. <c>--path</c> points at the project folder and has nothing to do with the
    /// game's flags; the document says so in a paragraph of its own.
    /// </summary>
    private static readonly string[] NotOurFlags = ["--path"];

    [Fact]
    public void CmdArgFlags_AreDocumented()
    {
        IReadOnlySet<string> documented = DocumentedFlags();
        FailureReport report = new(
            $"Flags declared in {RepositoryPaths.Relative(RepositoryPaths.CmdArgsDirectory)} but " +
            $"missing from Docs/Cli-args.md");

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
        HashSet<string> declared = DeclaredFlags()
            .Select(declaration => declaration.Literal.Token.ValueText)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new("Flags described in Docs/Cli-args.md that the code does not declare");

        foreach (string flag in DocumentedFlags().Order(StringComparer.Ordinal))
        {
            if (!declared.Contains(flag) && !NotOurFlags.Contains(flag))
            {
                report.Add($"'{flag}' — either it was renamed in Scripts/Content/CmdArgs/, or the row is stale");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// Read from the raw text rather than through MarkdownDocument: that class blanks out code spans on
    /// purpose, and here the flags are exactly what is inside them.
    /// </summary>
    private static IReadOnlySet<string> DocumentedFlags()
    {
        string text = File.ReadAllText(Path.Combine(RepositoryPaths.DocsDirectory, "Cli-args.md"));
        return DocumentedFlagRegex.Matches(text)
            .Select(match => match.Groups["flag"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<(CSharpFile File, LiteralExpressionSyntax Literal)> DeclaredFlags() =>
        RepositoryPaths.CmdArgsFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<LiteralExpressionSyntax>()
                .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                .Where(literal => literal.Token.ValueText.StartsWith(FlagPrefix, StringComparison.Ordinal))
                .Select(literal => (File: file, Literal: literal)));
}
