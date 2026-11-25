using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// <c>ChatSimulationFacade</c> takes every <c>IChatCommand</c> in the container, and <c>/help</c> every
/// <c>IListedChatCommand</c>. The layer is checked here and not only by the collection rules: they let a facade
/// take a collection of any layer it may take, so a <c>[Simulation]</c> command would pass them.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ChatCommandTests
{
    private const string ChatCommands = WorldLayers.WorldNamespace + ".Features.Chat.ChatCommands";
    private const string ChatCommand = ChatCommands + ".IChatCommand";
    private const string ListedChatCommand = ChatCommands + ".IListedChatCommand";
    private const string HelpCommand = ChatCommands + ".HelpChatCommandSimulationFacade";

    [Fact]
    public void ChatCommands_AreSimulationFacades()
    {
        FailureReport report = new("Chat commands outside the facade layer");
        GameAssembly game = GameAssembly.Instance;

        List<TypeDefinition> commands = game.Types
            .Where(type => type is { IsInterface: false, IsAbstract: false }
                           && WorldLayers.Implements(type, ChatCommand))
            .ToList();
        Assert.NotEmpty(commands);
        foreach (TypeDefinition command in commands)
        {
            if (WorldLayers.DeclaredLayer(command) != Layer.SimulationFacade)
            {
                report.Add($"{GameAssembly.Describe(command)}: is not marked [SimulationFacade]");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// <c>/help</c> takes every listed command, so it cannot be one itself; any other command that is not listed
    /// silently never shows in <c>/help</c>.
    /// </summary>
    [Fact]
    public void ChatCommands_AreListed_ExceptHelp()
    {
        FailureReport report = new("Chat commands /help does not list");

        IEnumerable<TypeDefinition> unlisted = GameAssembly.Instance.Types
            .Where(type => type is { IsInterface: false, IsAbstract: false }
                           && WorldLayers.Implements(type, ChatCommand)
                           && !WorldLayers.Implements(type, ListedChatCommand));
        Assert.Contains(unlisted, type => type.FullName == HelpCommand);
        foreach (TypeDefinition command in unlisted.Where(type => type.FullName != HelpCommand))
        {
            report.Add($"{GameAssembly.Describe(command)}: implements IChatCommand but not IListedChatCommand, " +
                       "so /help never lists it");
        }

        report.AssertEmpty();
    }
}
