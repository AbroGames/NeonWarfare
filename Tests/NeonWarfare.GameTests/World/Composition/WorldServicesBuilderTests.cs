using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.ClientNetwork;
using NeonWarfare.Scenes.World.CommandHandlers;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Presentations;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using NeonWarfare.Scenes.World.Simulations;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World.Composition;

// In the engine on purpose: the point is that MS.DI works inside a Godot process
[TestSuite]
public class WorldServicesBuilderTests
{
    private const WorldLayer Server =
        WorldLayer.Simulation | WorldLayer.CommandHandler | WorldLayer.ServerNetwork | WorldLayer.Query;

    private const WorldLayer Client =
        WorldLayer.Query | WorldLayer.Presentation | WorldLayer.ServerHudPresentation | WorldLayer.ClientNetwork;
    private const WorldLayer Host =
        Server | WorldLayer.Presentation | WorldLayer.ServerHudPresentation | WorldLayer.ClientNetwork;
    private const WorldLayer DedicatedWithServerHud =
        Server | WorldLayer.ServerHudPresentation | WorldLayer.Console | WorldLayer.ClientNetwork;
    private const WorldLayer HeadlessDedicated = Server;

    // Handler → facade → simulation are constructor-injected and ValidateOnBuild rejects a broken chain:
    // a present handler means all three are there, an absent ChatSimulation means none is

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Client_HasOnlyPresentation()
    {
        using ServiceProvider provider = Build(Client);

        AssertThat(provider.GetService<ChatSimulation>()).IsNull();
        AssertThat(provider.GetService<EventOutbox>()).IsNull();
        AssertThat(provider.GetService<PeerUidMap>()).IsNull();
        AssertThat(provider.GetService<ChatPresentation>()).IsNotNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_ServerConfigurations_HaveOutboxAndPeerMap()
    {
        foreach (WorldLayer layers in new[] { Host, DedicatedWithServerHud, HeadlessDedicated })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventOutbox>()).IsNotNull();
            AssertThat(provider.GetService<PeerUidMap>()).IsNotNull();
        }
    }

    // The host's own client is a peer like any other; the headless server has nobody to show events to
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyDedicatedWithServerHud_HasConsole()
    {
        foreach ((WorldLayer layers, bool hasConsole) in new[]
                 {
                     (Host, false),
                     (DedicatedWithServerHud, true),
                     (HeadlessDedicated, false),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetRequiredService<EventOutbox>().HasConsole).IsEqual(hasConsole);
        }
    }

    // The two give the same chat services: ChatPresentation is RequiredByServerHud
    [TestCase]
    [RequireGodotRuntime]
    public void Build_HostAndDedicatedWithServerHud_HaveSimulationAndPresentation()
    {
        foreach (WorldLayer layers in new[] { Host, DedicatedWithServerHud })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<SendChatMessageHandler>()).IsNotNull();
            AssertThat(provider.GetService<ChatPresentation>()).IsNotNull();
        }
    }

    // Wherever a Presentation is, received events have to reach it
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyConfigurationsWithPresentation_HaveEventDispatcher()
    {
        foreach ((WorldLayer layers, bool hasDispatcher) in new[]
                 {
                     (Client, true),
                     (Host, true),
                     (DedicatedWithServerHud, true),
                     (HeadlessDedicated, false),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventDispatcher>() != null).IsEqual(hasDispatcher);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_HeadlessDedicated_HasOnlySimulation()
    {
        using ServiceProvider provider = Build(HeadlessDedicated);

        AssertThat(provider.GetService<SendChatMessageHandler>()).IsNotNull();
        AssertThat(provider.GetService<ChatPresentation>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CreatesServicesNobodyRequests()
    {
        UnrequestedPresentation.Created = 0;

        using ServiceProvider provider = FixtureBuilder(typeof(UnrequestedPresentation)).Build(Host, Dependencies());

        AssertThat(UnrequestedPresentation.Created).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_HasQueries()
    {
        foreach (WorldLayer layers in new[] { Client, Host, DedicatedWithServerHud, HeadlessDedicated })
        {
            using ServiceProvider provider = FixtureBuilder(typeof(FixtureQuery)).Build(layers, Dependencies());

            // A bool: AssertThat dispatches dynamically and cannot bind a private fixture type
            AssertThat(provider.GetService<FixtureQuery>() != null).IsTrue();
        }
    }

    // The layer table lets facades take facades; no test of ours looks for cycles, the container does
    [TestCase]
    [RequireGodotRuntime]
    public void Build_FacadeCycle_Throws()
    {
        string message = "";
        try
        {
            FixtureBuilder(typeof(CyclicFacadeA), typeof(CyclicFacadeB))
                .Build(Host, Dependencies())
                .Dispose();
        }
        catch (AggregateException exception)
        {
            message = exception.Message;
        }

        AssertThat(message).Contains("circular dependency");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_OutsideTree_Succeeds()
    {
        GameWorld world = AutoFree(new GameWorld())!;

        AssertThat(world.InitPreReady(Host, Dependencies())).IsSame(world);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_InsideTree_Throws()
    {
        GameWorld world = AutoFree(new GameWorld())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);

        AssertThrown(() => world.InitPreReady(Host, Dependencies()))
            .IsInstanceOf<InvalidOperationException>();
    }

    private static ServiceProvider Build(WorldLayer layers) =>
        new WorldServicesBuilder().Build(layers, Dependencies());

    // The root gives the outbox of a ServerHud world its console and the dispatcher its handlers, so both come
    // with any fixture
    private static WorldServicesBuilder FixtureBuilder(params Type[] fixtures) =>
        new([..fixtures, typeof(EventOutbox), typeof(PeerUidMap), typeof(EventDispatcher)]);

    private static WorldDependencies Dependencies() =>
        new(TimeProvider.System, new PersistenceModel(), new SessionModel(), Codec());

    private static NetMessageCodec Codec() => new(NetMessageCodecTests.CreateMapping());

    [Query]
    private class FixtureQuery;

    [Simulation(Facade = true)]
    private class CyclicFacadeA(CyclicFacadeB other)
    {
        public CyclicFacadeB Other { get; } = other;
    }

    [Simulation(Facade = true)]
    private class CyclicFacadeB(CyclicFacadeA other)
    {
        public CyclicFacadeA Other { get; } = other;
    }

    // Stands for a Presentation with only event handlers: nothing takes it in a constructor
    [Presentation]
    private class UnrequestedPresentation
    {
        public static int Created;

        public UnrequestedPresentation() => Created++;
    }
}
