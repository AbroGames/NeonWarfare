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

    private TransportWorlds _worlds = null!;
    private FakeNetwork _network = null!;
    private RecordingLocalPlayerOwner _owner = null!;
    private List<byte[]> _snapshots = null!;
    private Action<byte[]> _onSnapshot = null!;
    private ClientTransport _transport = null!;

    [BeforeTest]
    public void SetUp()
    {
        _worlds = new TransportWorlds();
        _network = new FakeNetwork();
        _owner = new RecordingLocalPlayerOwner();
        _snapshots = [];
        _onSnapshot = _snapshots.Add;
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
    public void PacketReceived_WithoutAWorld_OnlyTheSnapshotGoesOn()
    {
        byte[] snapshot = [(byte) ServerPacketKind.Snapshot, 1];

        _network.Receive(ServerPeer, [(byte) ServerPacketKind.State, 1]);
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.Events, 1]);
        _network.Receive(ServerPeer, []);
        _network.Receive(ServerPeer, snapshot);

        AssertThat(_snapshots.Count).IsEqual(1);
        AssertThat(_snapshots[0]).ContainsExactly(snapshot);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_NotFromTheServer_IsDropped()
    {
        _network.Receive(OtherPeer, [(byte) ServerPacketKind.Snapshot, 1]);
        _network.Receive(OtherPeer, JoinRejectedPacket.Write(JoinRejectReason.UidInUse));

        AssertThat(_snapshots).IsEmpty();
        AssertThat(_owner.Rejections).IsEmpty();
    }

    // Before and after the World alike: the World would throw on it
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_JoinRejected_ReachesTheOwner_ABrokenOneIsDropped()
    {
        _network.Receive(ServerPeer, JoinRejectedPacket.Write(JoinRejectReason.UidInUse));
        _network.Receive(ServerPeer, [(byte) ServerPacketKind.JoinRejected]);
        Connect();
        _network.Receive(ServerPeer, JoinRejectedPacket.Write(JoinRejectReason.InternalError));

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
        var serverNetwork = new FakeNetwork();
        World server = AutoFree(new World())!;
        _worlds.Init(server, TestWorldSetups.Dedicated(), new ServerTransport(serverNetwork, server),
            new NoConnection());
        serverNetwork.Wire = (peerId, packet) =>
        {
            if (peerId == ClientPeer) _network.Receive(ServerPeer, packet);
        };
        _network.Wire = (_, packet) => serverNetwork.Receive(ClientPeer, packet);

        World? client = null;
        _onSnapshot = snapshot =>
        {
            client = AutoFree(new World())!;
            _worlds.Init(client, TestWorldSetups.RemoteClient(localPlayerOwner: _owner), new NoConnection(),
                _transport, new WorldOrigin.FromSnapshot(snapshot));
            _transport.Enter(client);
        };

        serverNetwork.Connect(ClientPeer);
        _network.ConnectToServer();
        TransportWorlds.Tick(server);
        return client!;
    }
}
