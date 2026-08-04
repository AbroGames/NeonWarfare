using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// The channel table of Docs/Networking.md against Consts.TransferChannel. A channel is written into an
/// [Rpc(...)] attribute as a number the enum position produces, so the document is not just a list of
/// names: the order of the rows is the numbering, and a member inserted in the middle silently
/// renumbers everything after it. The names are only ever seen inside a cast, which no reader of the
/// document has in front of them.
/// </summary>
public class NetworkingDocTests
{
    private const string DocumentName = "Networking.md";

    private const string TableHeading = "Transfer channels";

    private const string ChannelEnum = "TransferChannel";

    [Fact]
    public void TransferChannels_AreListedInTheTable()
    {
        IReadOnlySet<string> documented = DocumentedChannels().ToHashSet(StringComparer.Ordinal);
        FailureReport report = new($"Members of Consts.{ChannelEnum} missing from the table of Docs/{DocumentName}");

        foreach (string channel in DeclaredChannels())
        {
            if (!documented.Contains(channel))
            {
                report.Add($"{channel} — add a row saying what goes through it");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingTransferChannels()
    {
        IReadOnlySet<string> declared = DeclaredChannels().ToHashSet(StringComparer.Ordinal);
        FailureReport report = new($"Rows of Docs/{DocumentName} that Consts.{ChannelEnum} does not back");

        foreach (string channel in DocumentedChannels())
        {
            if (!declared.Contains(channel))
            {
                report.Add($"{channel} — no such member, the name is misspelled or the channel is gone");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The position of a member is the channel number Godot receives, so a table in a different order
    /// tells a reader the wrong number even while every name in it exists.
    /// </summary>
    [Fact]
    public void TableRows_FollowTheDeclarationOrder()
    {
        IReadOnlyList<string> declared = DeclaredChannels();
        IReadOnlyList<string> documented = DocumentedChannels();

        // Missing and unknown names are reported by the two checks above; comparing the order of lists
        // that do not hold the same names would only repeat them as a mismatch at every position.
        if (!declared.OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(documented.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            return;
        }

        Assert.True(declared.SequenceEqual(documented, StringComparer.Ordinal),
            $"Docs/{DocumentName}: the channel rows are in the order {string.Join(", ", documented)}, " +
            $"Consts.{ChannelEnum} declares {string.Join(", ", declared)}. The position of a row is the " +
            $"channel number, so the order is part of what the table says.");
    }

    /// <summary>
    /// What makes "the position is the channel number" true in the first place. An explicit value on a
    /// member breaks it without touching the table, and the document would go on claiming a numbering
    /// the enum no longer has.
    /// </summary>
    [Fact]
    public void TransferChannels_TakeTheirNumberFromTheirPosition()
    {
        CSharpFile file = CSharpFile.Load(RepositoryPaths.ConstsPath);
        FailureReport report = new($"Members of Consts.{ChannelEnum} with a number of their own");

        foreach (EnumMemberDeclarationSyntax member in ChannelEnumDeclaration(file).Members)
        {
            if (member.EqualsValue is not null)
            {
                report.Add($"{member.Identifier.ValueText} = {member.EqualsValue.Value} — drop the value, " +
                           $"or rewrite the table of Docs/{DocumentName} so that it names the numbers");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneChannelAndDescribeIt()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        FailureReport report = new($"Malformed rows of the channel table of Docs/{DocumentName}");

        foreach (IReadOnlyList<string> row in Table().Rows)
        {
            List<string> names = MarkdownDocument.CodeSpans(row[0]).ToList();
            if (names.Count != 1)
            {
                report.Add($"'{row[0]}' — the first cell must hold exactly one channel name in backticks");
                continue;
            }

            if (row[1].Length == 0)
            {
                report.Add($"{names[0]} — the row says nothing about what goes through the channel");
            }

            if (!seen.Add(names[0]))
            {
                report.Add($"{names[0]} — listed twice");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>The channel names of the table, in the order they are written.</summary>
    private static IReadOnlyList<string> DocumentedChannels() =>
        Table().Rows
            .Select(row => MarkdownDocument.CodeSpans(row[0]).FirstOrDefault())
            .OfType<string>()
            .ToList();

    /// <summary>The members of Consts.TransferChannel, in declaration order.</summary>
    private static IReadOnlyList<string> DeclaredChannels() =>
        ChannelEnumDeclaration(CSharpFile.Load(RepositoryPaths.ConstsPath))
            .Members.Select(member => member.Identifier.ValueText).ToList();

    private static EnumDeclarationSyntax ChannelEnumDeclaration(CSharpFile file) =>
        file.Nodes<EnumDeclarationSyntax>()
            .FirstOrDefault(candidate => candidate.Identifier.ValueText == ChannelEnum)
        ?? throw new InvalidOperationException(
            $"{file.RelativePath}: no enum {ChannelEnum}. It was renamed or moved, and " +
            $"Docs/{DocumentName} has nothing left to be checked against.");

    private static MarkdownTable Table() =>
        Document().TableUnder(TableHeading) ?? throw new InvalidOperationException(
            $"Docs/{DocumentName}: no table under '{TableHeading}'. Either the heading was renamed, or " +
            $"the list of channels is gone.");

    private static MarkdownDocument Document() =>
        MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, DocumentName));
}
