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
    private const string CommandsNamespace = WorldLayers.WorldNamespace + ".Infra.Server.Commands";
    private const string PeersNamespace = WorldLayers.WorldNamespace + ".Infra.Server.Peers";
    private const string JoinCommand = WorldLayers.WorldNamespace + ".Infra.Protocol.JoinRequestCommand";
    private const string PlayerHandler = CommandsNamespace + ".IPlayerCommandHandler`1";
    private const string SessionHandler = PeersNamespace + ".IPeerSessionHandler";

    /// <summary>
    /// <c>JoinRequestCommand</c> goes to the <c>IPeerSessionHandler</c>: the joining peer has no player yet.
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
                               "never calls — the join goes to IPeerSessionHandler");
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
    /// The server takes from a player only the commands with a player handler, and the join only from a peer that has
    /// not joined: <c>Game</c> sends it itself, before a remote client has a World.
    /// </summary>
    [Fact]
    public void SentCommands_HavePlayerHandler()
    {
        FailureReport report = new("Commands sent that the server never takes from a player");
        IReadOnlySet<string> playerHandlers = PlayerHandledCommands();

        foreach (CommandSends.Site site in CommandSends.All())
        {
            string where = GameAssembly.Describe(site.From);
            if (site.Command.FullName == JoinCommand)
            {
                report.Add($"{where}: sends {site.Command.Name}, which only Game sends");
            }
            else if (!playerHandlers.Contains(site.Command.FullName))
            {
                report.Add($"{where}: sends {site.Command.Name}, which has no " +
                           $"IPlayerCommandHandler<{site.Command.Name}>, so the server rejects it");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// <c>CommandHandlerRegistry.Register</c> takes at most one, but only at run time: without it a peer could
    /// neither join nor leave.
    /// </summary>
    [Fact]
    public void SessionHandler_HasExactlyOneImplementation()
    {
        List<TypeDefinition> implementations = HandlerTypes()
            .Where(type => WorldLayers.Implements(type, SessionHandler))
            .ToList();

        string found = string.Join(", ", implementations.Select(GameAssembly.Describe));
        Assert.True(implementations.Count == 1,
            $"{SessionHandler}: {implementations.Count} implementations, one expected ({found})");
    }

    /// <summary>
    /// The composition root passes the dispatcher only the services of the <c>[CommandHandler]</c> layer.
    /// </summary>
    [Fact]
    public void CommandHandlers_AreCommandHandlerLayer()
    {
        FailureReport report = new("Command handlers outside the CommandHandler layer");

        List<TypeDefinition> handlers = HandlerTypes()
            .Where(type => WorldLayers.Interfaces(type).Any(IsHandlerInterface))
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

    private static bool IsCommand(TypeDefinition type) =>
        type is { IsAbstract: false, IsNested: false } && WorldLayers.DerivesFrom(type, WorldLayers.CommandBase);

    /// <summary>The full names of the commands some type of the game implements the player handler for.</summary>
    private static IReadOnlySet<string> PlayerHandledCommands() =>
        HandlerTypes()
            .SelectMany(WorldLayers.Interfaces)
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

    private static bool IsHandlerInterface(TypeReference implemented) =>
        implemented.GetElementType().FullName is PlayerHandler or SessionHandler;
}
