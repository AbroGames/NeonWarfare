using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The network command whitelist is built from the handlers <c>CommandDispatcher</c> was given, so a command
/// without a handler, or a handler the composition root never passed, is silently dropped at run time.
/// </summary>
[Collection(GameAssembly.Collection)]
public class CommandHandlerTests
{
    private const string CommandsNamespace = WorldLayers.WorldNamespace + ".Commands";
    private const string HandlersNamespace = WorldLayers.WorldNamespace + ".CommandHandlers";
    private const string CommandBase = CommandsNamespace + ".Command";
    private const string JoinCommand = CommandsNamespace + ".JoinRequestCommand";
    private const string PlayerHandler = HandlersNamespace + ".IPlayerCommandHandler`1";
    private const string DedicatedWindowHandler = HandlersNamespace + ".IDedicatedWindowCommandHandler`1";
    private const string JoinHandler = HandlersNamespace + ".IJoinRequestHandler";
    private const string DedicatedWindowSender =
        WorldLayers.WorldNamespace + ".ServerNetwork.DedicatedWindowCommandSender";
    private const string SendMethod = "Send";

    /// <summary>
    /// <c>JoinRequestCommand</c> goes to the <c>IJoinRequestHandler</c>: the joining peer has no player yet.
    /// </summary>
    //TODO 015 require an IJoinRequestHandler implementation once the Join facade exists
    [Fact]
    public void EveryCommand_HasNetworkHandler()
    {
        FailureReport report = new("Commands without a network handler");
        GameAssembly game = GameAssembly.Instance;

        List<TypeDefinition> commands = game.Types.Where(IsCommand).ToList();
        Assert.Contains(commands, command => command.FullName == JoinCommand);
        IReadOnlySet<string> playerHandlers = HandledCommands(PlayerHandler);

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

    [Fact]
    public void DedicatedWindowCommands_HaveDedicatedWindowHandler()
    {
        FailureReport report = new("Commands sent from the dedicated window without a handler for it");
        GameAssembly game = GameAssembly.Instance;

        TypeDefinition sender = game.FindByName(DedicatedWindowSender)
                                ?? throw new InvalidOperationException($"{DedicatedWindowSender} is gone");
        // Nothing sends yet, and a renamed Send would leave the rule nothing to check
        Assert.Contains(sender.Methods, method => method.Name == SendMethod && method.HasGenericParameters);
        IReadOnlySet<string> windowHandlers = HandledCommands(DedicatedWindowHandler);

        foreach (MethodDefinition method in game.Types.SelectMany(type => type.Methods).Where(m => m.HasBody))
        {
            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not GenericInstanceMethod
                    {
                        Name: SendMethod, DeclaringType.FullName: DedicatedWindowSender
                    } send)
                {
                    continue;
                }

                TypeReference sent = send.GenericArguments[0];
                if (game.Find(sent) is not { IsAbstract: false } command)
                {
                    report.Add($"{GameAssembly.Describe(method)}: sends {sent.FullName}, not a concrete command " +
                               "type, so its handler cannot be checked");
                }
                else if (!windowHandlers.Contains(command.FullName))
                {
                    report.Add($"{GameAssembly.Describe(method)}: sends {command.Name}, which has no " +
                               $"IDedicatedWindowCommandHandler<{command.Name}>");
                }
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
            .Where(type => Interfaces(type).Any(implemented => HandlerInterface(implemented) != null))
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

    /// <summary>The full names of the commands some type of the game implements the handler interface for.</summary>
    private static IReadOnlySet<string> HandledCommands(string handlerInterface) =>
        HandlerTypes()
            .SelectMany(Interfaces)
            .OfType<GenericInstanceType>()
            .Where(generic => generic.ElementType.FullName == handlerInterface)
            .Select(generic => generic.GenericArguments[0] is GenericParameter parameter
                ? throw new NotSupportedException(
                    $"{handlerInterface} is implemented for the type parameter {parameter.Name} of " +
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

    private static string? HandlerInterface(TypeReference implemented)
    {
        string name = implemented.GetElementType().FullName;
        return name is PlayerHandler or DedicatedWindowHandler or JoinHandler ? name : null;
    }
}
