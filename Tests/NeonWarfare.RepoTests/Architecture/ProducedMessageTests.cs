using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// Commands and events are found by their base type, so one that nothing sends or nothing handles still compiles and
/// is still mapped: it changes the protocol hash and is dead code nobody notices. And only the Simulation publishes
/// events: a node or a Presentation publishing one would make the outbox a bus.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ProducedMessageTests
{
    private const string GameNamespace = "NeonWarfare.";
    private const string EventBase = WorldLayers.WorldNamespace + ".Infra.Protocol.Event";
    private const string CommandBase = WorldLayers.WorldNamespace + ".Infra.Protocol.Command";
    private const string CloneMethod = "<Clone>$";
    private const string EventHandlerAttribute =
        WorldLayers.WorldNamespace + ".Infra.Client.Events.EventHandlerAttribute";

    /// <summary>
    /// <c>JoinRequestCommand</c> is sent by <c>Game.SendJoinRequest</c> before a remote client has a World, so no
    /// command sender ever carries it; it must still be built somewhere.
    /// </summary>
    private static readonly string[] SentPastCommandSenders =
        [WorldLayers.WorldNamespace + ".Infra.Protocol.JoinRequestCommand"];

    [Fact]
    public void Events_AreConstructedOnlyInSimulation()
    {
        FailureReport report = new("Events constructed outside the Simulation, or never");
        IReadOnlyDictionary<string, List<Construction>> constructions = Constructions();

        List<TypeDefinition> events = Messages(EventBase).ToList();
        Assert.NotEmpty(events);
        foreach (TypeDefinition @event in events)
        {
            List<Construction> found = constructions.GetValueOrDefault(@event.FullName, [])
                // The record's own Clone, behind a 'with' expression
                .Where(construction => construction.Owner != @event || construction.Method.Name != CloneMethod)
                .ToList();
            if (found.Count == 0)
            {
                report.Add($"{GameAssembly.Describe(@event)}: nothing constructs it, so nothing publishes it");
            }

            foreach (Construction construction in found)
            {
                Layer? layer = WorldLayers.LayerOf(construction.Owner);
                if (layer is not { } owner || !WorldLayers.SimulationLayers.Contains(owner))
                {
                    report.Add($"{GameAssembly.Describe(construction.Method)}: constructs {@event.Name} in " +
                               $"{(layer?.ToString() ?? "no layer")}; only a [Simulation] or a [SimulationFacade] " +
                               "publishes events");
                }
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void Events_HaveEventHandler()
    {
        FailureReport report = new("Events no [EventHandler] takes");
        IReadOnlySet<string> handled = GameAssembly.Instance.Types
            .SelectMany(type => type.Methods)
            .Where(method => method.Parameters.Count == 1 && method.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName == EventHandlerAttribute))
            .Select(method => method.Parameters[0].ParameterType.FullName)
            .ToHashSet(StringComparer.Ordinal);

        CrossCheck.ReportMissing(
            report,
            Messages(EventBase).Select(@event => @event.FullName),
            handled,
            @event => $"{@event}: the client reads it and drops it — add an [EventHandler] in a Presentation, " +
                      "or delete the event");

        report.AssertEmpty();
    }

    [Fact]
    public void Commands_AreSent()
    {
        FailureReport report = new("Commands nothing sends");
        IReadOnlySet<string> sent = CommandSends.All()
            .Select(site => site.Command.FullName)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlySet<string> constructed = Constructions().Keys.ToHashSet(StringComparer.Ordinal);

        List<TypeDefinition> commands = Messages(CommandBase).ToList();
        Assert.NotEmpty(commands);
        foreach (TypeDefinition command in commands)
        {
            if (SentPastCommandSenders.Contains(command.FullName))
            {
                if (!constructed.Contains(command.FullName))
                {
                    report.Add($"{GameAssembly.Describe(command)}: nothing constructs it, so nothing sends it");
                }
            }
            else if (!sent.Contains(command.FullName))
            {
                report.Add($"{GameAssembly.Describe(command)}: no Send<{command.Name}> of a command sender — " +
                           "send it from the screen, or delete the command and its handler");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void SentPastCommandSenders_ListsCommands()
    {
        IReadOnlySet<string> commands = Messages(CommandBase)
            .Select(command => command.FullName)
            .ToHashSet(StringComparer.Ordinal);
        CrossCheck.AssertExemptionsExist(
            nameof(SentPastCommandSenders), SentPastCommandSenders, commands.Contains, "no such command");
    }

    private sealed record Construction(TypeDefinition Owner, MethodDefinition Method);

    // The same rule WorldServicesBuilder and CommandHandlerTests use to find events and commands
    private static IEnumerable<TypeDefinition> Messages(string baseType) =>
        GameAssembly.Instance.Types.Where(type =>
            type is { IsNested: false, IsAbstract: false } && WorldLayers.DerivesFrom(type, baseType));

    /// <summary>
    /// Every <c>newobj</c> of the game's own code, by the full name of the type it creates. The MessagePack source
    /// generator writes its formatters into its own namespace, and their <c>Deserialize</c> reads an event, it
    /// does not publish one.
    /// </summary>
    private static IReadOnlyDictionary<string, List<Construction>> Constructions()
    {
        Dictionary<string, List<Construction>> found = new(StringComparer.Ordinal);
        foreach (TypeDefinition type in GameAssembly.Instance.Types.Where(type =>
                     GameAssembly.Outermost(type).Namespace.StartsWith(GameNamespace, StringComparison.Ordinal)))
        {
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code != Code.Newobj || instruction.Operand is not MethodReference ctor)
                    {
                        continue;
                    }

                    string created = ctor.DeclaringType.GetElementType().FullName;
                    if (!found.TryGetValue(created, out List<Construction>? list))
                    {
                        list = [];
                        found.Add(created, list);
                    }
                    list.Add(new Construction(type, method));
                }
            }
        }

        return found;
    }
}
