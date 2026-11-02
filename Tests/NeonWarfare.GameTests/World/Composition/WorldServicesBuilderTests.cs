using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.ClientNetwork;
using NeonWarfare.Scenes.World.CommandHandlers;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;
using NeonWarfare.Scenes.World.Presentations;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.Queries;
using NeonWarfare.Scenes.World.ServerNetwork;
using NeonWarfare.Scenes.World.Simulations;
using NeonWarfare.Scenes.World.Simulations.ChatCommands;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World.Composition;

// In the engine on purpose: the point is that MS.DI works inside a Godot process
[TestSuite]
public class WorldServicesBuilderTests
{
    private const WorldLayer Dedicated =
        WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler | WorldLayer.ServerNetwork
        | WorldLayer.Query;

    private const WorldLayer Client = WorldLayer.Query | WorldLayer.Presentation | WorldLayer.ClientNetwork;
    private const WorldLayer Host = Dedicated | WorldLayer.Presentation | WorldLayer.ClientNetwork;

    // Handler → facade → simulation are constructor-injected and ValidateOnBuild rejects a broken chain:
    // a present handler means all three are there, an absent ChatSimulation means none is
    [TestCase]
    [RequireGodotRuntime]
    public void Build_EachConfiguration_HasItsSimulationAndPresentation()
    {
        foreach ((WorldLayer layers, bool hasSimulation, bool hasPresentation) in new[]
                 {
                     (Client, false, true),
                     (Host, true, true),
                     (Dedicated, true, false),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<SendChatMessageHandler>() != null).IsEqual(hasSimulation);
            AssertThat(provider.GetService<ChatSimulation>() != null).IsEqual(hasSimulation);
            AssertThat(provider.GetService<ChatPresentation>() != null).IsEqual(hasPresentation);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyServerConfigurations_HaveOutboxPeerMapAndCommandQueue()
    {
        foreach ((WorldLayer layers, bool isServer) in new[]
                 {
                     (Client, false),
                     (Host, true),
                     (Dedicated, true),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventOutbox>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<PeerUidMap>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<CommandInbox>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<CommandDispatcher>() != null).IsEqual(isServer);
        }
    }

    // Wherever a Presentation is, received events have to reach it, and ChatPresentation posts to the mailbox
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyConfigurationsWithPresentation_HaveEventDispatcherAndHudMailbox()
    {
        foreach ((WorldLayer layers, bool hasPresentation) in new[]
                 {
                     (Client, true),
                     (Host, true),
                     (Dedicated, false),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventDispatcher>() != null).IsEqual(hasPresentation);
            AssertThat(provider.GetService<HudMailbox>() != null).IsEqual(hasPresentation);
        }
    }

    // The commands reach the facade through Register, not the constructor: nothing else would notice a missed call
    [TestCase]
    [RequireGodotRuntime]
    public void Build_ServerConfigurations_RegisterChatCommands()
    {
        foreach (WorldLayer layers in new[] { Host, Dedicated })
        {
            using ServiceProvider provider = Build(layers);

            IEnumerable<IChatCommand> commands = provider.GetRequiredService<ChatSimulationFacade>().Commands;
            AssertThat(commands.Select(command => command.GetType()))
                .Contains(typeof(HelpChatCommandSimulationFacade));
        }

        using ServiceProvider client = Build(Client);
        AssertThat(client.GetService<ChatSimulationFacade>()).IsNull();
        AssertThat(client.GetService<HelpChatCommandSimulationFacade>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CreatesServicesNobodyRequests()
    {
        UnrequestedPresentation.Created = 0;

        using ServiceProvider provider = Build(FixtureBuilder(typeof(UnrequestedPresentation)), Host);

        AssertThat(UnrequestedPresentation.Created).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_HasQueries()
    {
        foreach (WorldLayer layers in new[] { Client, Host, Dedicated })
        {
            using ServiceProvider provider = Build(FixtureBuilder(typeof(FixtureQuery)), layers);

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
            Build(FixtureBuilder(typeof(CyclicFacadeA), typeof(CyclicFacadeB)), Host).Dispose();
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

        AssertThat(world.InitPreReady(Host, Dependencies(), WorldOrigin.New)).IsSame(world);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_InsideTree_Throws()
    {
        GameWorld world = AutoFree(new GameWorld())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);

        AssertThrown(() => world.InitPreReady(Host, Dependencies(), WorldOrigin.New))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_NewWorld_SpawnsBothStoragesUnderTheWorld()
    {
        foreach (WorldLayer layers in new[] { Host, Dedicated })
        {
            GameWorld world = AutoFree(new GameWorld())!;

            world.InitPreReady(layers, Dependencies(), WorldOrigin.New);

            AssertThat(world.GetChildren().OfType<PersistenceStorage>().Count()).IsEqual(1);
            AssertThat(world.GetChildren().OfType<SessionStorage>().Count()).IsEqual(1);
        }
    }

    // Spawning is the origin's business: a loaded world gets its entities from the save, a client from the server
    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_SpawnsNothing()
    {
        foreach (WorldLayer layers in new[] { Client, Host, Dedicated })
        {
            Node root = AutoFree(new Node())!;

            using ServiceProvider provider =
                new WorldServicesBuilder().Build(layers, Dependencies(), new WorldRoot(root));

            AssertThat(provider.GetRequiredService<IEntityFinder>().GetAll<Node>()).IsEmpty();
            AssertThat(root.GetChildCount()).IsEqual(0);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Client_HasNoSpawnerNorNetIdGenerator()
    {
        using ServiceProvider provider = Build(Client);

        AssertThat(provider.GetService<EntitySpawner>()).IsNull();
        AssertThat(provider.GetService<NetIdGenerator>()).IsNull();
        AssertThat(provider.GetService<NewWorldSimulationFacade>()).IsNull();
    }

    private static ServiceProvider Build(WorldLayer layers) => Build(new WorldServicesBuilder(), layers);

    private static ServiceProvider Build(WorldServicesBuilder builder, WorldLayer layers) =>
        builder.Build(layers, Dependencies(), new WorldRoot(AutoFree(new Node())!));

    // The root gives the event dispatcher and the command dispatcher their handlers, the inbox its whitelist and the
    // chat commands facade its commands, so all of them come with any fixture, with what the dispatcher takes
    private static WorldServicesBuilder FixtureBuilder(params Type[] fixtures) =>
        new([
            ..fixtures, typeof(EventOutbox), typeof(PeerUidMap), typeof(EventDispatcher), typeof(CommandInbox),
            typeof(CommandDispatcher), typeof(ChatSimulation), typeof(ChatSimulationFacade),
            typeof(PersistenceStorageQuery),
        ]);

    private static WorldDependencies Dependencies() =>
        new(TimeProvider.System, Codec(), new ManualFrameProvider(), AutoFree(TestWorldScenes.Create())!,
            new RecordingClientsConnection());

    private static NetMessageCodec Codec() => new(NetMessageCodecTests.CreateMapping(), []);

    [Query]
    private class FixtureQuery;

    [SimulationFacade]
    private class CyclicFacadeA(CyclicFacadeB other)
    {
        public CyclicFacadeB Other { get; } = other;
    }

    [SimulationFacade]
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
