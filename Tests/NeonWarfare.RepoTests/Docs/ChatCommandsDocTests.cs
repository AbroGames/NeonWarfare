using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// The command table of Docs/Chat-and-commands.md is the only list of what a player can type. Commands
/// register themselves — the composition root hands every created one to the chat facade — so nothing
/// anywhere names them together, and a new command or a renamed one leaves the table behind without a
/// single failure to show for it. The name a command answers to is a string literal inside its class,
/// which is exactly the kind of thing a document repeats and then stops matching.
/// </summary>
public class ChatCommandsDocTests
{
    private const string DocumentName = "Chat-and-commands.md";

    private const string TableHeading = "Chat commands";

    private const string CommandInterface = "IChatCommand";

    private const string NameProperty = "Name";

    private const string AdminProperty = "RequiresAdmin";

    private const string AdminRights = "admin";

    private const string EveryoneRights = "everyone";

    private const int CommandColumn = 0;

    private const int ClassColumn = 1;

    private const int RightsColumn = 2;

    [Fact]
    public void CommandClasses_AreListedInTheTable()
    {
        IReadOnlyDictionary<string, ChatCommand> declared = DeclaredCommands();
        IReadOnlySet<string> documented = DocumentedCommands()
            .Select(row => row.Class)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new($"Command classes missing from the table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            declared.Keys.Order(StringComparer.Ordinal),
            documented,
            @class => $"{@class} — add a row for '/{declared[@class].Command}'");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingCommandClasses()
    {
        FailureReport report = new($"Rows of Docs/{DocumentName} that no {CommandInterface} backs");

        CrossCheck.ReportMissing(
            report,
            DocumentedCommands().Select(row => row.Class),
            DeclaredCommands().Keys.ToHashSet(StringComparer.Ordinal),
            @class => $"{@class} — renamed or deleted, the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameTheCommandTheClassHandles()
    {
        IReadOnlyDictionary<string, ChatCommand> declared = DeclaredCommands();

        FailureReport report = new($"Rows of Docs/{DocumentName} whose command name is not what the class answers to");

        foreach ((string command, string @class) in DocumentedCommands())
        {
            // A row pointing at nothing is reported by TableRows_PointToExistingCommandClasses; here it
            // would only produce a second failure saying the same thing.
            if (!declared.TryGetValue(@class, out ChatCommand? declaredCommand))
            {
                continue;
            }

            if (!string.Equals(command, declaredCommand.Command, StringComparison.Ordinal))
            {
                report.Add($"{@class} — the table says '/{command}', " +
                           $"{NameProperty} returns '{declaredCommand.Command}'");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_StateTheRightsTheClassRequires()
    {
        IReadOnlyDictionary<string, ChatCommand> declared = DeclaredCommands();

        FailureReport report = new($"Rows of Docs/{DocumentName} that promise the wrong rights");

        foreach (IReadOnlyList<string> row in Table().Rows)
        {
            string? @class = MarkdownTable.SingleCodeSpan(row[ClassColumn]);
            if (@class is null || !declared.TryGetValue(@class, out ChatCommand? command))
            {
                continue;
            }

            string rights = row[RightsColumn];
            string expected = command.RequiresAdmin ? AdminRights : EveryoneRights;

            if (!string.Equals(rights, expected, StringComparison.Ordinal))
            {
                report.Add($"{@class} — the table says '{rights}', {AdminProperty} returns " +
                           $"{command.RequiresAdmin.ToString().ToLowerInvariant()}, so it must say '{expected}'");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneCommandAndOneClass()
    {
        MarkdownTable table = Table();
        FailureReport report = new($"Malformed rows of the command table of Docs/{DocumentName}");

        DocTableChecks.SingleCodeSpanPerRow(table, report, ClassColumn, "class name");

        foreach (IReadOnlyList<string> row in table.Rows)
        {
            string? command = MarkdownTable.SingleCodeSpan(row[CommandColumn]);
            if (command is null || !command.StartsWith('/'))
            {
                report.Add($"'{row[CommandColumn]}' — the {table.Header[CommandColumn]} cell must hold " +
                           $"exactly one '/command' in backticks");
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
            string? command = MarkdownTable.SingleCodeSpan(row[CommandColumn]);
            string? @class = MarkdownTable.SingleCodeSpan(row[ClassColumn]);

            if (command is null || @class is null || !command.StartsWith('/'))
            {
                continue;
            }

            yield return (command[1..].Split(' ')[0], @class);
        }
    }

    /// <summary>
    /// Every implementation of IChatCommand, by class name, with the name it answers to and the rights it
    /// asks for. Both come from expression-bodied properties returning a literal — that is how all of them
    /// are written, and a command computing either would be an unreadable command in the first place.
    /// </summary>
    private static IReadOnlyDictionary<string, ChatCommand> DeclaredCommands()
    {
        Dictionary<string, ChatCommand> commands = new(StringComparer.Ordinal);

        foreach ((CSharpFile file, ClassDeclarationSyntax declaration) in CommandDeclarations())
        {
            commands[declaration.Identifier.ValueText] = new ChatCommand(
                ReturnedLiteral(file, declaration, NameProperty),
                ReturnedLiteral(file, declaration, AdminProperty) == "true");
        }

        return commands;
    }

    /// <summary>
    /// Every class under Src/ that names IChatCommand in its base list — the whole of Src/, because the
    /// composition root scans the whole assembly. The interface is matched by its rightmost identifier, so a
    /// qualified name counts as well. A command reaching the interface through a base class of its own is
    /// not found; there is none.
    /// </summary>
    private static IEnumerable<(CSharpFile File, ClassDeclarationSyntax Declaration)> CommandDeclarations() =>
        CSharpFile.LoadAll().SelectMany(file => file.Nodes<ClassDeclarationSyntax>()
            .Where(declaration => declaration.BaseList?.Types
                .Any(type => RightmostIdentifier(type.Type) == CommandInterface) ?? false)
            .Select(declaration => (file, declaration)));

    private static string RightmostIdentifier(TypeSyntax type) =>
        type switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => type.ToString(),
        };

    /// <summary>The literal an expression-bodied property returns, as it is written in the source.</summary>
    private static string ReturnedLiteral(
        CSharpFile file, ClassDeclarationSyntax declaration, string propertyName)
    {
        PropertyDeclarationSyntax? property = declaration.Members.OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(candidate => candidate.Identifier.ValueText == propertyName);

        if (property?.ExpressionBody?.Expression is not LiteralExpressionSyntax literal)
        {
            throw new InvalidOperationException(
                $"{file.RelativePath}: {declaration.Identifier.ValueText}.{propertyName} is not an " +
                $"expression-bodied property returning a literal. Every command is written that way, and " +
                $"the document can only be checked against a value that is readable without running the game.");
        }

        return literal.Token.ValueText;
    }

    private static MarkdownTable Table() =>
        Document().Section(TableHeading)
            .RequireTable("the list of commands is gone", "Command", "Class", "Rights");

    private static MarkdownDocument Document() => MarkdownDocument.LoadDoc(DocumentName);
}

/// <summary>One chat command as the code defines it: the name it answers to and the rights.</summary>
internal sealed record ChatCommand(string Command, bool RequiresAdmin);
