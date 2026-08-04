using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// The command table of Docs/Chat-and-commands.md is the only list of what a player can type. Commands
/// register themselves — the service scans the assembly — so nothing anywhere names them together, and
/// a new processor or a renamed one leaves the table behind without a single failure to show for it.
/// The name a command answers to is a string literal inside its class, which is exactly the kind of
/// thing a document repeats and then stops matching.
/// </summary>
public class ChatCommandsDocTests
{
    private const string DocumentName = "Chat-and-commands.md";

    private const string TableHeading = "Chat and commands";

    private const string ProcessorInterface = "ICommandProcessor";

    private const string CommandMethod = "GetCommand";

    private const string AdminMethod = "IsRequiringAdmin";

    private const string AdminRights = "admin";

    private const string EveryoneRights = "everyone";

    /// <summary>
    /// Not a row of the table: its GetCommand() is empty on purpose — it is the fallback the service
    /// calls when no processor matched, and a row would put a command into the list that nobody can
    /// type. The document describes it in the paragraph under the table instead, and
    /// <see cref="ExcludedCommands_AreDescribedInTheText"/> keeps that paragraph honest.
    /// </summary>
    private static readonly string[] NotDocumentedAsRow = ["NotFoundCommand"];

    [Fact]
    public void CommandClasses_AreListedInTheTable()
    {
        IReadOnlySet<string> documented = DocumentedCommands()
            .Select(row => row.Class)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new($"Command classes missing from the table of Docs/{DocumentName}");

        foreach (CommandProcessor processor in DeclaredCommands())
        {
            if (!documented.Contains(processor.Class) && !NotDocumentedAsRow.Contains(processor.Class))
            {
                report.Add($"{processor.Class} — add a row for '/{processor.Command}', " +
                           $"or list it in NotDocumentedAsRow with the reason");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingCommandClasses()
    {
        IReadOnlySet<string> declared = DeclaredCommands()
            .Select(processor => processor.Class)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new($"Rows of Docs/{DocumentName} that no ICommandProcessor backs");

        foreach ((_, string @class) in DocumentedCommands())
        {
            if (!declared.Contains(@class))
            {
                report.Add($"{@class} — renamed or deleted, the row is stale");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameTheCommandTheClassHandles()
    {
        IReadOnlyDictionary<string, CommandProcessor> declared = DeclaredCommands()
            .ToDictionary(processor => processor.Class, StringComparer.Ordinal);

        FailureReport report = new($"Rows of Docs/{DocumentName} whose command name is not what the class answers to");

        foreach ((string command, string @class) in DocumentedCommands())
        {
            // A row pointing at nothing is reported by TableRows_PointToExistingCommandClasses; here it
            // would only produce a second failure saying the same thing.
            if (!declared.TryGetValue(@class, out CommandProcessor? processor))
            {
                continue;
            }

            if (!string.Equals(command, processor.Command, StringComparison.Ordinal))
            {
                report.Add($"{@class} — the table says '/{command}', {CommandMethod}() returns '{processor.Command}'");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_StateTheRightsTheClassRequires()
    {
        IReadOnlyDictionary<string, CommandProcessor> declared = DeclaredCommands()
            .ToDictionary(processor => processor.Class, StringComparer.Ordinal);

        FailureReport report = new($"Rows of Docs/{DocumentName} that promise the wrong rights");

        foreach (IReadOnlyList<string> row in Table().Rows)
        {
            string? @class = MarkdownDocument.CodeSpans(row[1]).FirstOrDefault();
            if (@class is null || !declared.TryGetValue(@class, out CommandProcessor? processor))
            {
                continue;
            }

            string rights = row[2].Trim();
            string expected = processor.RequiresAdmin ? AdminRights : EveryoneRights;

            if (!string.Equals(rights, expected, StringComparison.Ordinal))
            {
                report.Add($"{@class} — the table says '{rights}', {AdminMethod}() returns " +
                           $"{processor.RequiresAdmin.ToString().ToLowerInvariant()}, so it must say '{expected}'");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneCommandAndOneClass()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        FailureReport report = new($"Malformed rows of the command table of Docs/{DocumentName}");

        foreach (IReadOnlyList<string> row in Table().Rows)
        {
            List<string> commands = MarkdownDocument.CodeSpans(row[0]).ToList();
            List<string> classes = MarkdownDocument.CodeSpans(row[1]).ToList();

            if (commands.Count != 1 || !commands[0].StartsWith('/'))
            {
                report.Add($"'{row[0]}' — the first cell must hold exactly one '/command' in backticks");
            }

            if (classes.Count != 1)
            {
                report.Add($"'{row[1]}' — the second cell must hold exactly one class name in backticks");
                continue;
            }

            if (!seen.Add(classes[0]))
            {
                report.Add($"{classes[0]} — listed twice");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// An excluded class still has to be written about somewhere, or the exception turns into a way of
    /// hiding a command from the document altogether.
    /// </summary>
    [Fact]
    public void ExcludedCommands_AreDescribedInTheText()
    {
        MarkdownDocument document = Document();
        IReadOnlySet<string> mentioned = document.Lines
            .SelectMany(MarkdownDocument.CodeSpans)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new($"Command classes left out of the table and never explained in Docs/{DocumentName}");

        foreach (string excluded in NotDocumentedAsRow)
        {
            if (!mentioned.Contains(excluded))
            {
                report.Add($"{excluded} — say in the text what it is and why it has no row");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The rows as they are written: the command without its leading slash and without the arguments
    /// spelled out after it (<c>/admin {add\|remove} &lt;nickname&gt;</c> is the <c>admin</c> command),
    /// and the class from the second cell. Malformed rows are dropped here and reported by
    /// <see cref="TableRows_NameOneCommandAndOneClass"/>.
    /// </summary>
    private static IEnumerable<(string Command, string Class)> DocumentedCommands()
    {
        foreach (IReadOnlyList<string> row in Table().Rows)
        {
            string? command = MarkdownDocument.CodeSpans(row[0]).FirstOrDefault();
            string? @class = MarkdownDocument.CodeSpans(row[1]).FirstOrDefault();

            if (command is null || @class is null || !command.StartsWith('/'))
            {
                continue;
            }

            yield return (command[1..].Split(' ')[0], @class);
        }
    }

    /// <summary>
    /// Every implementation of ICommandProcessor, with the name it answers to and the rights it asks
    /// for. Both come from expression-bodied methods returning a literal — that is how all of them are
    /// written, and a processor computing either would be an unreadable command in the first place.
    /// </summary>
    private static IReadOnlyList<CommandProcessor> DeclaredCommands()
    {
        List<CommandProcessor> processors = [];

        foreach (string path in RepositoryPaths.CommandProcessorFiles())
        {
            CSharpFile file = CSharpFile.Load(path);

            foreach (ClassDeclarationSyntax declaration in file.Nodes<ClassDeclarationSyntax>())
            {
                bool implements = declaration.BaseList?.Types
                    .Any(type => type.Type.ToString() == ProcessorInterface) ?? false;

                if (!implements)
                {
                    continue;
                }

                processors.Add(new CommandProcessor(
                    declaration.Identifier.ValueText,
                    ReturnedLiteral(file, declaration, CommandMethod),
                    ReturnedLiteral(file, declaration, AdminMethod) == "true"));
            }
        }

        return processors;
    }

    /// <summary>The literal an expression-bodied method returns, as it is written in the source.</summary>
    private static string ReturnedLiteral(
        CSharpFile file, ClassDeclarationSyntax declaration, string methodName)
    {
        MethodDeclarationSyntax? method = declaration.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(candidate => candidate.Identifier.ValueText == methodName);

        if (method?.ExpressionBody?.Expression is not LiteralExpressionSyntax literal)
        {
            throw new InvalidOperationException(
                $"{file.RelativePath}: {declaration.Identifier.ValueText}.{methodName}() is not an " +
                $"expression-bodied method returning a literal. Every command is written that way, and " +
                $"the document can only be checked against a value that is readable without running the game.");
        }

        return literal.Token.ValueText;
    }

    /// <summary>
    /// The command table, with its shape checked before anything reads a cell by index: a column
    /// dropped or reordered has to say so, not surface as an index out of range three tests later.
    /// </summary>
    private static MarkdownTable Table()
    {
        MarkdownTable table = Document().TableUnder(TableHeading) ?? throw new InvalidOperationException(
            $"Docs/{DocumentName}: no table under '{TableHeading}'. Either the heading was renamed, or " +
            $"the list of commands is gone.");

        string[] expected = ["Command", "Class", "Rights"];
        if (!table.Header.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Docs/{DocumentName}:{table.Line}: the command table must have the columns " +
                $"{string.Join(" | ", expected)}, it has {string.Join(" | ", table.Header)}.");
        }

        return table;
    }

    private static MarkdownDocument Document() =>
        MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, DocumentName));
}

/// <summary>One chat command as the code defines it: the class, the name it answers to, the rights.</summary>
internal sealed record CommandProcessor(string Class, string Command, bool RequiresAdmin);
