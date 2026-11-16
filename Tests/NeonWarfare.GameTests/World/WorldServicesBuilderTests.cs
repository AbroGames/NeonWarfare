using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.ClientReplication;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Hud;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Replication;
using RepliCAT;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World;

// In the engine on purpose: the point is that MS.DI works inside a Godot process
[TestSuite]
public class WorldServicesBuilderTests
{
    private const string SaveFileName = "save";

    // Handler → facade → simulation are constructor-injected and ValidateOnBuild rejects a broken chain:
    // a present handler means all three are there, an absent ChatSimulation means none is
    [TestCase]
    [RequireGodotRuntime]
    public void Build_EachConfiguration_HasItsSimulationAndPresentation()
    {
        foreach ((WorldLayer layers, bool hasSimulation, bool hasPresentation) in new[]
                 {
                     (WorldLayer.Client, false, true),
                     (WorldLayer.Host, true, true),
                     (WorldLayer.Dedicated, true, false),
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
                     (WorldLayer.Client, false),
                     (WorldLayer.Host, true),
                     (WorldLayer.Dedicated, true),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventOutbox>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<PeerUidMap>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<CommandInbox>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<CommandDispatcher>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<CommandHandlerRegistry>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<PeerSessions>() != null).IsEqual(isServer);
        }
    }

    // Wherever a Presentation is, received events have to reach it, and ChatPresentation posts to the mailbox
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyConfigurationsWithPresentation_HaveEventDispatcherAndHudMailbox()
    {
        foreach ((WorldLayer layers, bool hasPresentation) in new[]
                 {
                     (WorldLayer.Client, true),
                     (WorldLayer.Host, true),
                     (WorldLayer.Dedicated, false),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<EventDispatcher>() != null).IsEqual(hasPresentation);
            AssertThat(provider.GetService<HudMailbox>() != null).IsEqual(hasPresentation);
        }
    }

    // The host's Simulation already wrote the state a remote client applies
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyRemoteClient_AppliesStatePackets_OnlyServers_WriteThem()
    {
        foreach ((WorldLayer layers, bool applies, bool writes) in new[]
                 {
                     (WorldLayer.Client, true, false),
                     (WorldLayer.Host, false, true),
                     (WorldLayer.Dedicated, false, true),
                 })
        {
            using ServiceProvider provider = Build(layers);

            AssertThat(provider.GetService<StateApplier>() != null).IsEqual(applies);
            AssertThat(provider.GetService<StateReplicator>() != null).IsEqual(writes);
        }
    }

    // The commands reach the facade through Register, not the constructor: nothing else would notice a missed call
    [TestCase]
    [RequireGodotRuntime]
    public void Build_ServerConfigurations_RegisterChatCommands()
    {
        foreach (WorldLayer layers in new[] { WorldLayer.Host, WorldLayer.Dedicated })
        {
            using ServiceProvider provider = Build(layers);

            IEnumerable<IChatCommand> commands = provider.GetRequiredService<ChatSimulationFacade>().Commands;
            AssertThat(commands.Select(command => command.GetType()))
                .Contains(typeof(HelpChatCommandSimulationFacade));
        }

        using ServiceProvider client = Build(WorldLayer.Client);
        AssertThat(client.GetService<ChatSimulationFacade>()).IsNull();
        AssertThat(client.GetService<HelpChatCommandSimulationFacade>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CreatesServicesNobodyRequests()
    {
        UnrequestedPresentation.Created = 0;

        using ServiceProvider provider = Build(FixtureBuilder(typeof(UnrequestedPresentation)), WorldLayer.Host);

        AssertThat(UnrequestedPresentation.Created).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_HasQueries()
    {
        foreach (WorldLayer layers in new[] { WorldLayer.Client, WorldLayer.Host, WorldLayer.Dedicated })
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
            Build(FixtureBuilder(typeof(CyclicFacadeA), typeof(CyclicFacadeB)), WorldLayer.Host).Dispose();
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

        AssertThat(world.InitPreReady(
                WorldLayer.Host, Dependencies(WorldLayer.Host), new WorldOrigin.NewWorld(SaveFileName)))
            .IsSame(world);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_InsideTree_Throws()
    {
        GameWorld world = AutoFree(new GameWorld())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);

        AssertThrown(() => world.InitPreReady(
                WorldLayer.Host, Dependencies(WorldLayer.Host), new WorldOrigin.NewWorld(SaveFileName)))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_NewWorld_SpawnsBothStoragesUnderTheWorld()
    {
        foreach (WorldLayer layers in new[] { WorldLayer.Host, WorldLayer.Dedicated })
        {
            GameWorld world = AutoFree(new GameWorld())!;

            world.InitPreReady(layers, Dependencies(layers), new WorldOrigin.NewWorld(SaveFileName));

            AssertThat(world.GetChildren().OfType<PlayersStorage>().Count()).IsEqual(1);
            AssertThat(world.GetChildren().OfType<PlayersSessionStorage>().Count()).IsEqual(1);
        }
    }

    // Spawning is the origin's business: a loaded world gets its entities from the save, a client from the server
    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_SpawnsNothing()
    {
        foreach (WorldLayer layers in new[] { WorldLayer.Client, WorldLayer.Host, WorldLayer.Dedicated })
        {
            Node root = AutoFree(new Node())!;

            using ServiceProvider provider =
                new WorldServicesBuilder().Build(layers, Dependencies(layers), new WorldRoot(root));

            AssertThat(provider.GetRequiredService<IEntityFinder>().GetAll<Node>()).IsEmpty();
            AssertThat(root.GetChildCount()).IsEqual(0);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Client_HasNoSpawnerNorNetIdGenerator()
    {
        using ServiceProvider provider = Build(WorldLayer.Client);

        AssertThat(provider.GetService<EntitySpawner>()).IsNull();
        AssertThat(provider.GetService<NetIdGenerator>()).IsNull();
        AssertThat(provider.GetService<NewWorldSimulationFacade>()).IsNull();
    }

    // A remote client has no save file
    [TestCase]
    [RequireGodotRuntime]
    public void Build_WithoutSaveFiles_OnlyRemoteClient()
    {
        var root = new WorldRoot(AutoFree(new Node())!);

        using ServiceProvider client = new WorldServicesBuilder()
            .Build(WorldLayer.Client, Dependencies(WorldLayer.Client) with { SaveFiles = null! }, root);
        AssertThat(client.GetService<ISaveFiles>()).IsNull();
        foreach (WorldLayer server in new[] { WorldLayer.Host, WorldLayer.Dedicated })
        {
            WorldDependencies noSaveFiles = Dependencies(server) with { SaveFiles = null! };
            AssertThrown(() => new WorldServicesBuilder().Build(server, noSaveFiles, root))
                .IsInstanceOf<ArgumentException>();
        }
    }

    // A dedicated server has no player of its own, and every World with a Presentation has one
    [TestCase]
    [RequireGodotRuntime]
    public void Build_LocalPlayer_OnlyAndAlwaysWithPresentation()
    {
        var root = new WorldRoot(AutoFree(new Node())!);
        WorldDependencies withLocalPlayer = Dependencies(WorldLayer.Host);

        foreach (WorldLayer layers in new[] { WorldLayer.Client, WorldLayer.Host })
        {
            WorldDependencies noLocalPlayer = Dependencies(layers) with { LocalPlayer = null! };
            AssertThrown(() => new WorldServicesBuilder().Build(layers, noLocalPlayer, root))
                .IsInstanceOf<ArgumentException>();
        }
        AssertThrown(() => new WorldServicesBuilder().Build(WorldLayer.Dedicated, withLocalPlayer, root))
            .IsInstanceOf<ArgumentException>();
    }

    // Without the owner the local player's join would leave the loading screen forever
    [TestCase]
    [RequireGodotRuntime]
    public void Build_LocalPlayerOwner_OnlyAndAlwaysWithLocalPlayer()
    {
        var root = new WorldRoot(AutoFree(new Node())!);

        foreach (WorldLayer layers in new[] { WorldLayer.Client, WorldLayer.Host })
        {
            WorldDependencies noOwner = Dependencies(layers) with { LocalPlayerOwner = null! };
            AssertThrown(() => new WorldServicesBuilder().Build(layers, noOwner, root))
                .IsInstanceOf<ArgumentException>();
        }
        WorldDependencies dedicatedWithOwner =
            Dependencies(WorldLayer.Dedicated) with { LocalPlayerOwner = new RecordingLocalPlayerOwner() };
        AssertThrown(() => new WorldServicesBuilder().Build(WorldLayer.Dedicated, dedicatedWithOwner, root))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Admin_OnlyAndAlwaysWithSimulation()
    {
        var root = new WorldRoot(AutoFree(new Node())!);

        foreach (WorldLayer layers in new[] { WorldLayer.Dedicated, WorldLayer.Host })
        {
            WorldDependencies noAdmin = Dependencies(layers) with { Admin = null! };
            AssertThrown(() => new WorldServicesBuilder().Build(layers, noAdmin, root))
                .IsInstanceOf<ArgumentException>();
        }
        WorldDependencies clientWithAdmin = Dependencies(WorldLayer.Client) with { Admin = new WorldAdmin(null!) };
        AssertThrown(() => new WorldServicesBuilder().Build(WorldLayer.Client, clientWithAdmin, root))
            .IsInstanceOf<ArgumentException>();
    }

    // The host's admin is the process itself, so only a dedicated server reports the admin leaving
    [TestCase]
    [RequireGodotRuntime]
    public void Build_DedicatedServerOwner_OnlyAndAlwaysOnDedicated()
    {
        var root = new WorldRoot(AutoFree(new Node())!);
        var owner = new RecordingDedicatedServerOwner();

        WorldDependencies noOwner = Dependencies(WorldLayer.Dedicated) with { DedicatedServerOwner = null! };
        AssertThrown(() => new WorldServicesBuilder().Build(WorldLayer.Dedicated, noOwner, root))
            .IsInstanceOf<ArgumentException>();
        foreach (WorldLayer layers in new[] { WorldLayer.Host, WorldLayer.Client })
        {
            WorldDependencies withOwner = Dependencies(layers) with { DedicatedServerOwner = owner };
            AssertThrown(() => new WorldServicesBuilder().Build(layers, withOwner, root))
                .IsInstanceOf<ArgumentException>();
        }
    }

    private static ServiceProvider Build(WorldLayer layers) => Build(new WorldServicesBuilder(), layers);

    private static ServiceProvider Build(WorldServicesBuilder builder, WorldLayer layers) =>
        builder.Build(layers, Dependencies(layers), new WorldRoot(AutoFree(new Node())!));

    // The root gives the event dispatcher and the command handler registry their handlers and the chat commands
    // facade its commands, so all of them come with any fixture, with what the chat takes
    private static WorldServicesBuilder FixtureBuilder(params Type[] fixtures) =>
        new([
            ..fixtures, typeof(EventOutbox), typeof(PeerUidMap), typeof(EventDispatcher),
            typeof(CommandHandlerRegistry), typeof(ChatSimulation), typeof(ChatSimulationFacade),
            typeof(PlayerQuery), typeof(PlayersStorageQuery), typeof(PlayersSessionStorageQuery),
        ]);

    private static WorldDependencies Dependencies(WorldLayer layers)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new(TimeProvider.System, Codec(),
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection(),
            new RecordingClientsConnection(), new RecordingSaveFiles(), TestWorldDependencies.LocalPlayer(layers),
            TestWorldDependencies.Admin(layers), TestWorldDependencies.DedicatedServerOwner(layers),
            TestWorldDependencies.LocalPlayerOwner(layers));
    }

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
