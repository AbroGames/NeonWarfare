using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// A service takes every service of a kind as <c>IEnumerable&lt;I&gt;</c>, and an element that is not in the
/// container, or does not implement <c>I</c>, is silently not in the collection: nothing fails, it just never
/// runs. Where membership is set by something other than the interface (an <c>[EventHandler]</c> method, the
/// <c>[CommandHandler]</c> layer), a rule ties the two together.
/// </summary>
[Collection(GameAssembly.Collection)]
public class CollectionInjectionTests
{
    [Fact]
    public void CollectionElements_AreLayerServices() =>
        ElementsWithoutLayer(GameAssembly.Instance).AssertEmpty();

    [Fact]
    public void EventHandlerDeclarers_AreEventHandlerOwners() =>
        EventHandlerDeclarersWithoutMarker(GameAssembly.Instance).AssertEmpty();

    [Fact]
    public void CommandHandlers_AreCommandHandlers() =>
        CommandHandlersWithoutMarker(GameAssembly.Instance).AssertEmpty();

    [Fact]
    public void CollectionElementWithoutLayer_IsReported()
    {
        GameAssembly fixture = GameAssembly.Compile(ArchitectureFixtures.LayerAttributes + """
            namespace Fixture
            {
                using NeonWarfare.Scenes.Worlds.Infra.Composition;

                public interface IPart;
                [Query] public class LayerPart : IPart;
                public class LostPart : IPart;
                public interface IUntaken;
                public class Untaken : IUntaken;
                [Query] public class Consumer(System.Collections.Generic.IEnumerable<IPart> parts);
            }
            """);

        AssertSingle(ElementsWithoutLayer(fixture), "LostPart");
    }

    [Fact]
    public void EventHandlerDeclarerWithoutMarker_IsReported()
    {
        GameAssembly fixture = GameAssembly.Compile(ArchitectureFixtures.LayerAttributes + """
            namespace Fixture
            {
                using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
                using NeonWarfare.Scenes.Worlds.Infra.Composition;

                [Presentation] public class Owner : IEventHandlerOwner { [EventHandler] private void Handle() { } }
                [Presentation] public class Lost { [EventHandler] private void Handle() { } }
                [Presentation] public class WithoutHandlers;
            }
            """);

        AssertSingle(EventHandlerDeclarersWithoutMarker(fixture), "Lost");
    }

    [Fact]
    public void CommandHandlerWithoutMarker_IsReported()
    {
        GameAssembly fixture = GameAssembly.Compile(ArchitectureFixtures.LayerAttributes + """
            namespace Fixture
            {
                using NeonWarfare.Scenes.Worlds.Infra.Composition;
                using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

                [CommandHandler] public class Handler : IPlayerCommandHandler<int>;
                [CommandHandler] public class Lost;
            }
            """);

        AssertSingle(CommandHandlersWithoutMarker(fixture), "Lost");
    }

    /// <summary>
    /// An implementation without a layer attribute is not in the container. Only the interfaces some layer service
    /// takes as a collection count: any other interface is no collection.
    /// </summary>
    private static FailureReport ElementsWithoutLayer(GameAssembly game)
    {
        FailureReport report = new("Implementations of a collection interface that are no layer service");

        IEnumerable<TypeReference> elements = game.Types
            .Where(type => WorldLayers.DeclaredLayer(type) != null)
            .SelectMany(type => type.Methods.Where(method => method.IsConstructor && !method.IsStatic))
            .SelectMany(constructor => constructor.Parameters)
            .Select(parameter => WorldLayers.CollectionElement(parameter.ParameterType))
            .OfType<TypeReference>()
            .DistinctBy(element => element.FullName);
        foreach (TypeReference element in elements)
        {
            foreach (TypeDefinition implementation in WorldLayers.Implementations(game, element)
                         .Where(implementation => WorldLayers.DeclaredLayer(implementation) == null))
            {
                report.Add($"{GameAssembly.Describe(implementation)}: implements {GameAssembly.ShortName(element)} " +
                           "but has no layer attribute, so it is never in the collection");
            }
        }

        return report;
    }

    private static FailureReport EventHandlerDeclarersWithoutMarker(GameAssembly game)
    {
        FailureReport report = new($"Types with an [EventHandler] that are not {WorldLayers.EventHandlerOwner}");

        IEnumerable<TypeDefinition> declarers = game.Types
            .Where(type => type.Methods.Any(WorldLayers.IsEventHandler)
                           && !WorldLayers.Implements(type, WorldLayers.EventHandlerOwner));
        foreach (TypeDefinition declarer in declarers)
        {
            report.Add($"{GameAssembly.Describe(declarer)}: declares an [EventHandler] but is no " +
                       "IEventHandlerOwner, so the dispatcher never gets it");
        }

        return report;
    }

    private static FailureReport CommandHandlersWithoutMarker(GameAssembly game)
    {
        FailureReport report = new($"[CommandHandler] services that are not {WorldLayers.CommandHandler}");

        IEnumerable<TypeDefinition> handlers = game.Types
            .Where(type => WorldLayers.DeclaredLayer(type) == Layer.CommandHandler
                           && !WorldLayers.Implements(type, WorldLayers.CommandHandler));
        foreach (TypeDefinition handler in handlers)
        {
            report.Add($"{GameAssembly.Describe(handler)}: is a [CommandHandler] but no ICommandHandler, so the " +
                       "registry never gets it");
        }

        return report;
    }

    private static void AssertSingle(FailureReport report, string typeName)
    {
        string failure = Assert.Single(report.Failures);
        Assert.StartsWith(typeName + ":", failure);
    }
}
