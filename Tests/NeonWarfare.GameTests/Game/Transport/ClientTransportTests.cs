using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Transport;

[TestSuite]
public class ClientTransportTests
{
    private const int ServerPeer = 1;
    private const int ClientPeer = 2;
    private const int OtherPeer = 3;
    private const string OtherUid = "OtherOther-Oooooooooo";

    private TransportWorlds _worlds = null!;
    private FakeNetwork _network = null!;
    private RecordingLocalPlayerOwner _owner = null!;
    private List<byte[]> _snapshots = null!;
    private Action<ReadOnlyMemory<byte>> _onSnapshot = null!;
    private ClientTransport _transport = null!;
    private FakeNetwork _serverNetwork = null!;
    private World _server = null!;

    [BeforeTest]
    public void SetUp()
    {
        _worlds = new TransportWorlds();
        _network = new FakeNetwork();
        _owner = new RecordingLocalPlayerOwner();
        _snapshots = [];
        _onSnapshot = RecordSnapshot;
        _transport = new ClientTransport(
            _network, _worlds.Codec, TestWorldSetups.LocalPlayer(), _owner, snapshot => _onSnapshot(snapshot));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ConnectedToServer_SendsTheJoinOfTheLocalPlayer()
    {
        _network.ConnectToServer();

        FakeNetwork.Sent sent = _network.Packets.Single();
        AssertThat(sent.PeerId).IsEqual(ServerPeer);
        AssertThat(sent.Packet).ContainsExactly(
            _worlds.Codec.Encode(TestWorldSetups.LocalPlayer().ToJoinRequest(_worlds.Codec.ProtocolHash)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_WithoutAWorld_OnlyTheSnapshotBodyGoesOn()
    {
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.State, 1]);
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.Events, 1]);
        _network.Receive(ServerPeer, []);
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.Snapshot, 1, 2]);

        AssertThat(_snapshots.Count).IsEqual(1);
        AssertThat(_snapshots[0]).ContainsExactly(1, 2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_NotFromTheServer_IsDropped()
    {
        _network.Receive(OtherPeer, [(byte) ServerPacketKind.Snapshot, 1]);
        _network.Receive(OtherPeer, Rejection(JoinRejectReason.UidInUse));

        AssertThat(_snapshots).IsEmpty();
        AssertThat(_owner.Rejections).IsEmpty();
    }

    // Before and after the World alike: the World would throw on it
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_JoinRejected_ReachesTheOwner_ABrokenOneIsDropped()
    {
        _network.Receive(ServerPeer, Rejection(JoinRejectReason.UidInUse));
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.JoinRejected]);
        Connect();
        _network.Receive(ServerPeer, Rejection(JoinRejectReason.InternalError));

        AssertThat(_owner.Rejections).ContainsExactly(JoinRejectReason.UidInUse, JoinRejectReason.InternalError);
    }

    // The whole join: the snapshot builds the World inside the call, and the events packet right after it reaches it
    [TestCase]
    [RequireGodotRuntime]
    public void Enter_ThePacketsAfterItReachTheWorld()
    {
        World client = Connect();

        AssertThat(_owner.JoinedCount).IsEqual(1);
        AssertThat(client.Get<LocalPlayerPresentation>().Player.Uid).IsEqual(TestWorldSetups.LocalPlayerUid);
    }

    // Every later join changes the online players: the state packet reaches the World
    [TestCase]
    [RequireGodotRuntime]
    public void Enter_StatePacketsReachTheWorld()
    {
        World client = Connect();

        _serverNetwork.Connect(OtherPeer);
        _serverNetwork.Receive(OtherPeer,
            _worlds.Codec.Encode(TestWorldSetups.LocalPlayer(OtherUid).ToJoinRequest(_worlds.Codec.ProtocolHash)));
        TransportWorlds.Tick(_server);

        AssertThat(client.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Uid))
            .ContainsExactlyInAnyOrder(TestWorldSetups.LocalPlayerUid, OtherUid);
    }

    // The server sends one snapshot per join: a second one is a broken packet, logged and dropped
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_SnapshotWithAWorld_IsDropped()
    {
        Connect();
        _onSnapshot = RecordSnapshot;

        _network.Receive(ServerPeer, [(byte) ServerPacketKind.Snapshot, 1]);

        AssertThat(_snapshots).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Enter_Twice_Throws()
    {
        World client = Connect();

        AssertThrown(() => _transport.Enter(client)).IsInstanceOf<InvalidOperationException>();
    }

    // A dedicated server over the same fake wire: the client is its peer 2, the server is the client's peer 1
    private World Connect()
    {
        _serverNetwork = new FakeNetwork();
        _server = AutoFree(new World())!;
        _worlds.Init(_server, TestWorldSetups.Dedicated(), new ServerTransport(_serverNetwork, _server),
            new NoConnection());
        _serverNetwork.Wire = (peerId, packet) =>
        {
            if (peerId == ClientPeer) _network.Receive(ServerPeer, packet);
        };
        _network.Wire = (_, packet) => _serverNetwork.Receive(ClientPeer, packet);

        World? client = null;
        _onSnapshot = snapshot =>
        {
            client = AutoFree(new World())!;
            _worlds.Init(client, TestWorldSetups.RemoteClient(localPlayerOwner: _owner), new NoConnection(),
                _transport, new WorldOrigin.FromSnapshot(snapshot));
            _transport.Enter(client);
        };

        _serverNetwork.Connect(ClientPeer);
        _network.ConnectToServer();
        TransportWorlds.Tick(_server);
        return client!;
    }

    private void RecordSnapshot(ReadOnlyMemory<byte> snapshot) => _snapshots.Add(snapshot.ToArray());

    private static byte[] Rejection(JoinRejectReason reason)
    {
        var packet = new ArrayBufferWriter<byte>();
        ServerPackets.WriteJoinRejected(reason, packet);
        return packet.WrittenSpan.ToArray();
    }
}
