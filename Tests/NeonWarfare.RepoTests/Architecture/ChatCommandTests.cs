using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// A chat command reaches <c>ChatSimulationFacade</c> only through <c>Register</c>, which the composition
/// root calls with the created services: a command that is not a world service silently does not exist.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ChatCommandTests
{
    private const string ChatCommand = WorldLayers.WorldNamespace + ".Features.Chat.ChatCommands.IChatCommand";

    [Fact]
    public void ChatCommands_AreSimulationFacades()
    {
        FailureReport report = new("Chat commands outside the facade layer");
        GameAssembly game = GameAssembly.Instance;

        List<TypeDefinition> commands = game.Types
            .Where(type => type is { IsInterface: false, IsAbstract: false } && Implements(type, ChatCommand))
            .ToList();
        Assert.NotEmpty(commands);
        foreach (TypeDefinition command in commands)
        {
            if (WorldLayers.DeclaredLayer(command) != Layer.SimulationFacade)
            {
                report.Add($"{GameAssembly.Describe(command)}: is not marked [SimulationFacade], so the " +
                           "composition root never registers it");
            }
        }

        report.AssertEmpty();
    }

    // Cecil lists only the interfaces a type declares itself
    private static bool Implements(TypeDefinition type, string interfaceName)
    {
        for (TypeDefinition? current = type;
             current != null;
             current = current.BaseType == null ? null : GameAssembly.Instance.Find(current.BaseType))
        {
            if (current.Interfaces.Any(implementation => implementation.InterfaceType.FullName == interfaceName))
            {
                return true;
            }
        }

        return false;
    }
}
