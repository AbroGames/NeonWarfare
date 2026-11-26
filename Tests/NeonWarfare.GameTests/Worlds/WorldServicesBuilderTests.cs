using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Client.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Presentation;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Replication;
using NeonWarfare.Scenes.Worlds.Ports;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds;

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
        foreach ((WorldSetup setup, bool hasSimulation, bool hasPresentation) in
                 new (WorldSetup, bool, bool)[]
                 {
                     (TestWorldSetups.RemoteClient(), false, true),
                     (TestWorldSetups.Host(), true, true),
                     (TestWorldSetups.Dedicated(), true, false),
                 })
        {
            using ServiceProvider provider = Build(setup);

            AssertThat(provider.GetService<SendChatMessageHandler>() != null).IsEqual(hasSimulation);
            AssertThat(provider.GetService<ChatSimulation>() != null).IsEqual(hasSimulation);
            AssertThat(provider.GetService<ChatPresentation>() != null).IsEqual(hasPresentation);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyServerConfigurations_HaveOutboxPeerMapAndCommandQueue()
    {
        foreach ((WorldSetup setup, bool isServer) in new (WorldSetup, bool)[]
                 {
                     (TestWorldSetups.RemoteClient(), false),
                     (TestWorldSetups.Host(), true),
                     (TestWorldSetups.Dedicated(), true),
                 })
        {
            using ServiceProvider provider = Build(setup);

            AssertThat(provider.GetService<EventOutbox>() != null).IsEqual(isServer);
            AssertThat(provider.GetService<PeerStateTable>() != null).IsEqual(isServer);
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
        foreach ((WorldSetup setup, bool hasPresentation) in new (WorldSetup, bool)[]
                 {
                     (TestWorldSetups.RemoteClient(), true),
                     (TestWorldSetups.Host(), true),
                     (TestWorldSetups.Dedicated(), false),
                 })
        {
            using ServiceProvider provider = Build(setup);

            AssertThat(provider.GetService<EventDispatcher>() != null).IsEqual(hasPresentation);
            AssertThat(provider.GetService<HudMailbox>() != null).IsEqual(hasPresentation);
        }
    }

    // The host's Simulation already wrote the state a remote client applies
    [TestCase]
    [RequireGodotRuntime]
    public void Build_OnlyRemoteClient_AppliesStatePackets_OnlyServers_WriteThem()
    {
        foreach ((WorldSetup setup, bool applies, bool writes) in new (WorldSetup, bool, bool)[]
                 {
                     (TestWorldSetups.RemoteClient(), true, false),
                     (TestWorldSetups.Host(), false, true),
                     (TestWorldSetups.Dedicated(), false, true),
                 })
        {
            using ServiceProvider provider = Build(setup);

            AssertThat(provider.GetService<StateApplier>() != null).IsEqual(applies);
            AssertThat(provider.GetService<StateReplicator>() != null).IsEqual(writes);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_ServerConfigurations_HaveChatCommands()
    {
        foreach (WorldSetup setup in new WorldSetup[] { TestWorldSetups.Host(), TestWorldSetups.Dedicated() })
        {
            using ServiceProvider provider = Build(setup);

            AssertThat(provider.GetServices<IChatCommand>().Select(command => command.GetType()))
                .Contains(typeof(HelpChatCommandSimulationFacade));
        }

        using ServiceProvider client = Build(TestWorldSetups.RemoteClient());
        AssertThat(client.GetService<ChatSimulationFacade>()).IsNull();
        AssertThat(client.GetService<HelpChatCommandSimulationFacade>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CreatesServicesNobodyRequests()
    {
        UnrequestedPresentation.Created = 0;

        using ServiceProvider provider =
            Build(FixtureBuilder(typeof(UnrequestedPresentation)), TestWorldSetups.Host());

        AssertThat(UnrequestedPresentation.Created).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_HasQueries()
    {
        foreach (WorldSetup setup in AllSetups())
        {
            using ServiceProvider provider = Build(FixtureBuilder(typeof(FixtureQuery)), setup);

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
            Build(FixtureBuilder(typeof(CyclicFacadeA), typeof(CyclicFacadeB)), TestWorldSetups.Host()).Dispose();
        }
        catch (AggregateException exception)
        {
            message = exception.Message;
        }

        AssertThat(message).Contains("circular dependency");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CollectionConsumer_GetsEveryImplementationOfItsLayers()
    {
        foreach ((WorldSetup setup, int count) in new (WorldSetup, int)[]
                 {
                     (TestWorldSetups.Host(), 3),
                     (TestWorldSetups.Dedicated(), 2),
                 })
        {
            using ServiceProvider provider = Build(
                FixtureBuilder(typeof(QueryPartA), typeof(QueryPartB), typeof(PresentationPart), typeof(PartConsumer)),
                setup);

            IReadOnlyList<IPart> parts = provider.GetRequiredService<PartConsumer>().Parts;
            AssertThat(parts.Count).IsEqual(count);
            AssertThat(parts.Contains(provider.GetRequiredService<QueryPartA>())).IsTrue();
            AssertThat(parts.Contains(provider.GetRequiredService<QueryPartB>())).IsTrue();
        }
    }

    // The consumer validates the collection in its constructor; the eager creation is what makes that fail the build.
    // A remote client: a server configuration creates every service anyway, to pass the chat commands
    [TestCase]
    [RequireGodotRuntime]
    public void Build_CollectionConsumerRejectingIt_Throws()
    {
        AssertThrown(() => Build(FixtureBuilder(typeof(QueryPartA), typeof(RejectingConsumer)),
                TestWorldSetups.RemoteClient()).Dispose())
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage(RejectingConsumer.Error);
    }

    // MS.DI would hand over one of the two without a word
    [TestCase]
    [RequireGodotRuntime]
    public void Build_SingleParameterOfAnInterfaceWithSeveralServices_Throws()
    {
        AssertThrown(() => Build(FixtureBuilder(typeof(QueryPartA), typeof(QueryPartB), typeof(SinglePartConsumer)),
                TestWorldSetups.RemoteClient()).Dispose())
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage(
                $"{typeof(SinglePartConsumer).FullName} takes a single IPart, which QueryPartA, QueryPartB implement: "
                + "take IEnumerable<IPart> instead");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_ServiceImplementingAPort_Throws()
    {
        AssertThrown(() => Build(FixtureBuilder(typeof(PortImpersonator)), TestWorldSetups.Host()).Dispose())
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_OutsideTree_Succeeds()
    {
        World world = AutoFree(new World())!;

        AssertThat(world.InitPreReady(
                TestWorldSetups.Host(), Dependencies(), new WorldOrigin.NewWorld(SaveFileName)))
            .IsSame(world);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_InsideTree_Throws()
    {
        World world = AutoFree(new World())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);

        AssertThrown(() => world.InitPreReady(
                TestWorldSetups.Host(), Dependencies(), new WorldOrigin.NewWorld(SaveFileName)))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_NewWorld_SpawnsBothStoragesUnderTheWorld()
    {
        foreach (WorldSetup setup in new WorldSetup[] { TestWorldSetups.Host(), TestWorldSetups.Dedicated() })
        {
            World world = AutoFree(new World())!;

            world.InitPreReady(setup, Dependencies(), new WorldOrigin.NewWorld(SaveFileName));

            AssertThat(world.GetChildren().OfType<PlayersStorage>().Count()).IsEqual(1);
            AssertThat(world.GetChildren().OfType<PlayersSessionStorage>().Count()).IsEqual(1);
        }
    }

    // Spawning is the origin's business: a loaded world gets its entities from the save, a client from the server
    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_SpawnsNothing()
    {
        foreach (WorldSetup setup in AllSetups())
        {
            Node root = AutoFree(new Node())!;

            using ServiceProvider provider =
                new WorldServicesBuilder().Build(setup, Dependencies(), new WorldRoot(root));

            AssertThat(provider.GetRequiredService<IEntityFinder>().GetAll<Node>()).IsEmpty();
            AssertThat(root.GetChildCount()).IsEqual(0);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Client_HasNoSpawnerNorNetIdGenerator()
    {
        using ServiceProvider provider = Build(TestWorldSetups.RemoteClient());

        AssertThat(provider.GetService<EntitySpawner>()).IsNull();
        AssertThat(provider.GetService<NetIdGenerator>()).IsNull();
        AssertThat(provider.GetService<NewWorldSimulationFacade>()).IsNull();
    }

    private static ServiceProvider Build(WorldSetup setup) => Build(new WorldServicesBuilder(), setup);

    private static ServiceProvider Build(WorldServicesBuilder builder, WorldSetup setup) =>
        builder.Build(setup, Dependencies(), new WorldRoot(AutoFree(new Node())!));

    private static WorldSetup[] AllSetups() =>
        [TestWorldSetups.RemoteClient(), TestWorldSetups.Host(), TestWorldSetups.Dedicated()];

    // The root gives the event dispatcher and the command handler registry their handlers and the chat commands
    // facade its commands, so all of them come with any fixture, with what the chat takes
    private static WorldServicesBuilder FixtureBuilder(params Type[] fixtures) =>
        new([
            ..fixtures, typeof(EventOutbox), typeof(PeerStateTable), typeof(EventDispatcher),
            typeof(CommandHandlerRegistry), typeof(ChatSimulation), typeof(ChatSimulationFacade),
            typeof(PlayerQuery), typeof(PlayersStorageQuery), typeof(PlayersSessionStorageQuery),
        ]);

    private static WorldDependencies Dependencies()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new(TimeProvider.System, Codec(),
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection(),
            new RecordingClientsConnection());
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

    private interface IPart;

    [Query]
    private class QueryPartA : IPart;

    [Query]
    private class QueryPartB : IPart;

    [Presentation]
    private class PresentationPart : IPart;

    [Query]
    private class PartConsumer(IEnumerable<IPart> parts)
    {
        public IReadOnlyList<IPart> Parts { get; } = parts.ToList();
    }

    [Query]
    private class SinglePartConsumer(IPart part)
    {
        public IPart Part { get; } = part;
    }

    [Query]
    private class RejectingConsumer
    {
        public const string Error = "rejected";

        public RejectingConsumer(IEnumerable<IPart> parts) => throw new InvalidOperationException(Error);
    }

    [Simulation]
    private class PortImpersonator : IServerOwner
    {
        public void AdminLeft() { }
    }
}
