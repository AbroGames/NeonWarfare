using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Features.Storages;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using static GdUnit4.Assertions;
using static NeonWarfare.Scenes.World.Features.Chat.ChatPresentation;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork;

// A host through the real composition root: the fake transport delivers the host's own packets to its own
// EventDispatcher, as Game does, so the host's chat is filled the way a remote client's is
[TestSuite]
public class ServerTickLoopTests
{
    private const WorldLayer Host = WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler
                                    | WorldLayer.ServerNetwork | WorldLayer.Query | WorldLayer.Presentation
                                    | WorldLayer.ClientNetwork;

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;

    private static readonly HashSet<Type> EventTypes = [typeof(ChatPlayerMessageEvent)];
    private static readonly ChatPlayerMessageEvent Hello = new(Now, "host", "Host", "hello");

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private ServiceProvider _provider = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _root = new Node();
        _clientsConnection = new RecordingClientsConnection();
    }

    [AfterTest]
    public void TearDown()
    {
        _provider.Dispose();
        _root.Free();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EventPublishedDuringTick_ReachesChatPresentationOnlyAtItsEnd()
    {
        Build(new WorldServicesBuilder([
            ..GameTypes().Where(type => type != typeof(SendChatMessageHandler)), typeof(PublishingHandler),
        ]));
        Command(HostPeer, "hello");
        AssertThat(Chat().Entries).IsEmpty();

        Loop().RunTick();

        AssertThat(_provider.GetRequiredService<PublishingHandler>().EntriesRightAfterPublish).IsEqual(0);
        AssertThat(Chat().Entries).ContainsExactly(new PlayerMessageEntry(Now, "host", "Host", "hello"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EventPublishedBetweenTicks_WaitsForTheNextTick()
    {
        Build(new WorldServicesBuilder());

        Outbox().PublishToAll(Hello);

        AssertThat(Chat().Entries).IsEmpty();
        AssertThat(_clientsConnection.Packets).IsEmpty();
        Loop().RunTick();
        AssertThat(Chat().Entries).ContainsExactly(new PlayerMessageEntry(Now, "host", "Host", "hello"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_SendsEveryPeerAnEventsPacket_HostIncluded()
    {
        Build(new WorldServicesBuilder());
        Outbox().PublishToAll(Hello);

        Loop().RunTick();

        AssertThat(_clientsConnection.Packets.Select(sent => sent.PeerId))
            .ContainsExactlyInAnyOrder(HostPeer, AlicePeer);
        foreach (RecordingClientsConnection.Sent sent in _clientsConnection.Packets)
        {
            AssertThat(sent.Packet[0]).IsEqual((byte) ServerPacketKind.Events);
            IReadOnlyList<object> events = _codec.ReadSection(sent.Packet.AsMemory(1..), EventTypes, out int read);
            AssertThat(read).IsEqual(sent.Packet.Length - 1);
            AssertThat(events).ContainsExactly(Hello);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_WithoutEvents_SendsNothing()
    {
        Build(new WorldServicesBuilder());

        Loop().RunTick();

        AssertThat(_clientsConnection.Packets).IsEmpty();
    }

    // Only the personal event of the second peer is pending, so an empty packet would show up for the host
    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_PeerWithoutEvents_GetsNoPacket()
    {
        Build(new WorldServicesBuilder());
        Outbox().PublishTo(Hello, Persistence().PlayerByUid["alice"]);

        Loop().RunTick();

        AssertThat(_clientsConnection.Packets.Select(sent => sent.PeerId)).ContainsExactly(AlicePeer);
    }

    // The failing peer is the remote one, so the host's chat shows whether the other peer still got its packet
    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_FailingPeer_DoesNotCostTheOthersTheirEvents()
    {
        Build(new WorldServicesBuilder());
        _clientsConnection.FailingPeer = AlicePeer;
        Outbox().PublishToAll(Hello);

        Loop().RunTick();

        AssertThat(Chat().Entries).HasSize(1);
        AssertThat(_clientsConnection.Packets.Select(sent => sent.PeerId)).ContainsExactly(HostPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CurrentTick_StartsAtZero_GrowsByOnePerTick()
    {
        Build(new WorldServicesBuilder());
        ServerTickLoop loop = Loop();

        AssertThat(loop.CurrentTick).IsEqual(0);
        loop.RunTick();
        AssertThat(loop.CurrentTick).IsEqual(1);
        loop.RunTick();
        AssertThat(loop.CurrentTick).IsEqual(2);
    }

    // Commands first, sending last: what a command publishes leaves in the same tick
    [TestCase]
    [RequireGodotRuntime]
    public void CommandQueuedBeforeTick_IsInChatAfterThatTick()
    {
        Build(new WorldServicesBuilder());
        Command(AlicePeer, "hi");

        Loop().RunTick();

        AssertThat(Chat().Entries).ContainsExactly(new PlayerMessageEntry(Now, "alice", "Alice", "hi"));
        AssertThat(_clientsConnection.Packets.Select(sent => sent.PeerId))
            .ContainsExactlyInAnyOrder(HostPeer, AlicePeer);
    }

    private void Build(WorldServicesBuilder builder)
    {
        _provider = builder.Build(
            Host,
            new WorldDependencies(
                new FixedTime(), _codec, new ManualFrameProvider(), _scenes, _clientsConnection),
            new WorldRoot(_root));
        _provider.GetRequiredService<NewWorldSimulationFacade>().Create();
        _clientsConnection.Loopback = _provider.GetRequiredService<EventDispatcher>().DispatchPacket;

        JoinDirectly("host", "Host", HostPeer);
        JoinDirectly("alice", "Alice", AlicePeer);
    }

    // Stands for the join of task 015
    private void JoinDirectly(string uid, string nick, int peerId)
    {
        Persistence().AddPlayer(uid).Nick = nick;
        _provider.GetRequiredService<SessionStorageQuery>().Model.OnlinePlayerUids.Add(uid);
        _provider.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        Outbox().AddPeer(peerId);
    }

    private void Command(int peerId, string text) =>
        _provider.GetRequiredService<CommandInbox>()
            .EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));

    private PersistenceModel Persistence() => _provider.GetRequiredService<PersistenceStorageQuery>().Model;

    private ServerTickLoop Loop() => _provider.GetRequiredService<ServerTickLoop>();

    private EventOutbox Outbox() => _provider.GetRequiredService<EventOutbox>();

    private ChatPresentation Chat() => _provider.GetRequiredService<ChatPresentation>();

    private static Type[] GameTypes() => typeof(WorldServicesBuilder).Assembly.GetTypes();

    private class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(Now);
    }

    // Publishes the way a Simulation does and looks at the host's chat right after, still inside the tick
    [CommandHandler]
    private class PublishingHandler(EventOutbox outbox, ChatPresentation chat)
        : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public int EntriesRightAfterPublish { get; private set; } = -1;

        public bool Validate(PlayerModel sender, SendChatMessageCommand command) => true;

        public void Process(PlayerModel sender, SendChatMessageCommand command)
        {
            outbox.PublishToAll(new ChatPlayerMessageEvent(Now, sender.Uid, sender.Nick, command.Text));
            EntriesRightAfterPublish = chat.Entries.Count;
        }
    }
}
