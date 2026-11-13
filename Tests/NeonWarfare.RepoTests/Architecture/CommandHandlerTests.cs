using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The network command whitelist is built from the handlers <c>CommandHandlerRegistry</c> was given, so a command
/// without a handler, or a handler the composition root never passed, is silently dropped at run time.
/// </summary>
[Collection(GameAssembly.Collection)]
public class CommandHandlerTests
{
    private const string ProtocolNamespace = WorldLayers.WorldNamespace + ".Infra.Protocol";
    private const string CommandsNamespace = WorldLayers.WorldNamespace + ".Infra.ServerNetwork.Commands";
    private const string PeersNamespace = WorldLayers.WorldNamespace + ".Infra.ServerNetwork.Peers";
    private const string CommandBase = ProtocolNamespace + ".Command";
    private const string JoinCommand = PeersNamespace + ".JoinRequestCommand";
    private const string PlayerHandler = CommandsNamespace + ".IPlayerCommandHandler`1";
    private const string JoinHandler = PeersNamespace + ".IJoinRequestHandler";
    private const string DisconnectedHandler = PeersNamespace + ".IPeerDisconnectedHandler";

    /// <summary>
    /// <c>JoinRequestCommand</c> goes to the <c>IJoinRequestHandler</c>: the joining peer has no player yet.
    /// </summary>
    [Fact]
    public void EveryCommand_HasNetworkHandler()
    {
        FailureReport report = new("Commands without a network handler");
        GameAssembly game = GameAssembly.Instance;

        List<TypeDefinition> commands = game.Types.Where(IsCommand).ToList();
        Assert.Contains(commands, command => command.FullName == JoinCommand);
        IReadOnlySet<string> playerHandlers = PlayerHandledCommands();

        foreach (TypeDefinition command in commands)
        {
            bool hasPlayerHandler = playerHandlers.Contains(command.FullName);
            if (command.FullName == JoinCommand)
            {
                if (hasPlayerHandler)
                {
                    report.Add($"{GameAssembly.Describe(command)}: has a player handler, which the dispatcher " +
                               "never calls — the join goes to IJoinRequestHandler");
                }
            }
            else if (!hasPlayerHandler)
            {
                report.Add($"{GameAssembly.Describe(command)}: no IPlayerCommandHandler<{command.Name}>, so the " +
                           "server rejects it from the network");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// <c>CommandHandlerRegistry.Register</c> takes one of each and only in a pair, but only at run time: without
    /// them a peer could neither join nor leave.
    /// </summary>
    [Fact]
    public void JoinAndDisconnectedHandlers_HaveExactlyOneImplementation()
    {
        FailureReport report = new("Join and peer disconnected handlers that are not exactly one");

        foreach (string handlerInterface in new[] { JoinHandler, DisconnectedHandler })
        {
            List<TypeDefinition> implementations = HandlerTypes()
                .Where(type => Interfaces(type).Any(implemented => implemented.FullName == handlerInterface))
                .ToList();
            if (implementations.Count != 1)
            {
                string found = string.Join(", ", implementations.Select(GameAssembly.Describe));
                report.Add($"{handlerInterface}: {implementations.Count} implementations, one expected ({found})");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The composition root passes the dispatcher only the services of the <c>[CommandHandler]</c> layer.
    /// </summary>
    [Fact]
    public void CommandHandlers_AreCommandHandlerLayer()
    {
        FailureReport report = new("Command handlers outside the CommandHandler layer");

        List<TypeDefinition> handlers = HandlerTypes()
            .Where(type => Interfaces(type).Any(IsHandlerInterface))
            .ToList();
        Assert.NotEmpty(handlers);
        foreach (TypeDefinition handler in handlers)
        {
            if (WorldLayers.DeclaredLayer(handler) != Layer.CommandHandler)
            {
                report.Add($"{GameAssembly.Describe(handler)}: is not marked [CommandHandler], so the dispatcher " +
                           "never gets it");
            }
        }

        report.AssertEmpty();
    }

    private static bool IsCommand(TypeDefinition type)
    {
        if (type.IsAbstract || type.IsNested)
        {
            return false;
        }

        for (TypeReference? current = type.BaseType;
             current != null;
             current = GameAssembly.Instance.Find(current)?.BaseType)
        {
            if (current.FullName == CommandBase)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The full names of the commands some type of the game implements the player handler for.</summary>
    private static IReadOnlySet<string> PlayerHandledCommands() =>
        HandlerTypes()
            .SelectMany(Interfaces)
            .OfType<GenericInstanceType>()
            .Where(generic => generic.ElementType.FullName == PlayerHandler)
            .Select(generic => generic.GenericArguments[0] is GenericParameter parameter
                ? throw new NotSupportedException(
                    $"{PlayerHandler} is implemented for the type parameter {parameter.Name} of " +
                    $"{parameter.Owner}: a handler with a generic base is not supported by this test")
                : generic.GenericArguments[0].FullName)
            .ToHashSet(StringComparer.Ordinal);

    // Abstract types are skipped: the dispatcher only ever gets instances
    private static IEnumerable<TypeDefinition> HandlerTypes() =>
        GameAssembly.Instance.Types.Where(type => !type.IsInterface && !type.IsAbstract);

    /// <summary>
    /// Cecil lists only the interfaces a type declares itself, while the dispatcher sees inherited ones too.
    /// </summary>
    private static IEnumerable<TypeReference> Interfaces(TypeDefinition type)
    {
        for (TypeDefinition? current = type;
             current != null;
             current = current.BaseType == null ? null : GameAssembly.Instance.Find(current.BaseType))
        {
            foreach (InterfaceImplementation implementation in current.Interfaces)
            {
                yield return implementation.InterfaceType;
            }
        }
    }

    private static bool IsHandlerInterface(TypeReference implemented) =>
        implemented.GetElementType().FullName is PlayerHandler or JoinHandler or DisconnectedHandler;
}
