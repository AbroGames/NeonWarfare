using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using RepliCAT;
using static GdUnit4.Assertions;
using static NeonWarfare.Scenes.World.Features.Chat.ChatPresentation;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork.Tick;

// A host through the real composition root: the fake transport delivers the host's own packets to its own
// EventDispatcher, as Game does, so the host's chat is filled the way a remote client's is. The state packets are
// covered by TickStateReplicationTests
[TestSuite]
public class ServerTickLoopTests
{

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;

    private static readonly HashSet<Type> EventTypes = [typeof(ChatPlayerMessageEvent)];
    private static readonly ChatPlayerMessageEvent Hello = new(Now, "host", "Host", "hello");

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private ManualTimeProvider _time = null!;
    private ServiceProvider _provider = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _root = new Node();
        _clientsConnection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _time = new ManualTimeProvider(Now);
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

        AssertThat(EventPackets().Select(sent => sent.PeerId)).ContainsExactlyInAnyOrder(HostPeer, AlicePeer);
        foreach (RecordingClientsConnection.Sent sent in EventPackets())
        {
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
        // The first tick sends the joined peers the whole state
        Loop().RunTick();
        _clientsConnection.Packets.Clear();

        Loop().RunTick();

        AssertThat(_clientsConnection.Packets).IsEmpty();
    }

    // Only the personal event of the second peer is pending, so an empty packet would show up for the host
    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_PeerWithoutEvents_GetsNoPacket()
    {
        Build(new WorldServicesBuilder());
        Outbox().PublishTo(Hello, "alice");

        Loop().RunTick();

        AssertThat(EventPackets().Select(sent => sent.PeerId)).ContainsExactly(AlicePeer);
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
        AssertThat(EventPackets().Select(sent => sent.PeerId)).ContainsExactlyInAnyOrder(HostPeer, AlicePeer);
    }

    // The deadline passes between the ticks: the join that came in time is processed before the timeout is checked
    [TestCase]
    [RequireGodotRuntime]
    public void RunTick_PeerNotJoinedByTheDeadline_IsDisconnectedAtTheEnd()
    {
        const int latePeer = 3;
        const int joiningPeer = 4;
        Build(new WorldServicesBuilder());
        var gatekeeper = _provider.GetRequiredService<PeerGatekeeper>();
        gatekeeper.OnPeerConnected(latePeer);
        gatekeeper.OnPeerConnected(joiningPeer);
        var join = new JoinRequestCommand(_codec.ProtocolHash, "BobBobBobB-Bbbbbbbbbb", "Bob", Colors.White);
        _provider.GetRequiredService<CommandInbox>().EnqueueFromPeer(joiningPeer, _codec.Encode(join));

        _time.Now += PeerGatekeeper.HandshakeTimeout;
        Loop().RunTick();

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(latePeer);
        AssertThat(_provider.GetRequiredService<PeerUidMap>().TryGetUid(joiningPeer, out _)).IsTrue();
    }

    private void Build(WorldServicesBuilder builder)
    {
        _provider = builder.Build(
            WorldLayer.Host,
            new WorldDependencies(
                _time, _codec, new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), _scenes,
                TestWorldScenes.CreateCatalog(_scenes),
                _clientsConnection, _clientsConnection, new RecordingSaveFiles(),
                TestWorldDependencies.LocalPlayer(WorldLayer.Host),
                TestWorldDependencies.Admin(WorldLayer.Host)),
            new WorldRoot(_root));
        _provider.GetRequiredService<NewWorldSimulationFacade>().Create();
        _clientsConnection.Loopback = _provider.GetRequiredService<EventDispatcher>().DispatchPacket;

        JoinDirectly("host", "Host", HostPeer);
        JoinDirectly("alice", "Alice", AlicePeer);
    }

    // Past the join, whose events would mix with the ones under test
    private void JoinDirectly(string uid, string nick, int peerId)
    {
        Players().AddPlayer(uid).Nick = nick;
        _provider.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids.Add(uid);
        _provider.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        Outbox().AddPeer(peerId);
    }

    private void Command(int peerId, string text) =>
        _provider.GetRequiredService<CommandInbox>()
            .EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));

    private PlayersModel Players() => _provider.GetRequiredService<PlayersStorageQuery>().Model;

    private IEnumerable<RecordingClientsConnection.Sent> EventPackets() =>
        _clientsConnection.Packets.Where(sent => sent.Packet[0] == (byte) ServerPacketKind.Events);

    private ServerTickLoop Loop() => _provider.GetRequiredService<ServerTickLoop>();

    private EventOutbox Outbox() => _provider.GetRequiredService<EventOutbox>();

    private ChatPresentation Chat() => _provider.GetRequiredService<ChatPresentation>();

    private static Type[] GameTypes() => typeof(WorldServicesBuilder).Assembly.GetTypes();

    // Publishes the way a Simulation does and looks at the host's chat right after, still inside the tick
    [CommandHandler]
    private class PublishingHandler(EventOutbox outbox, ChatPresentation chat, PlayerQuery players)
        : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public int EntriesRightAfterPublish { get; private set; } = -1;

        public bool Validate(string senderUid, SendChatMessageCommand command) => true;

        public void Process(string senderUid, SendChatMessageCommand command)
        {
            string nick = players.Get(senderUid).Nick;
            outbox.PublishToAll(new ChatPlayerMessageEvent(Now, senderUid, nick, command.Text));
            EntriesRightAfterPublish = chat.Entries.Count;
        }
    }
}
