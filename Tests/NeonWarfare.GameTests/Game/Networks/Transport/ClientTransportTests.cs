using NeonWarfare.Scripts;
using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Game.Networks.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Networks.Transport;

[TestSuite]
public class ClientTransportTests
{
    private const int ServerPeer = 1;
    private const int ClientPeer = 2;
    private const int OtherPeer = 3;
    private static readonly PlayerUid OtherUid = new("OtherOther-Oooooooooo");

    private TransportWorlds _worlds = null!;
    private FakeNetwork _network = null!;
    private RecordingLocalPlayerOwner _owner = null!;
    private List<(long Tick, byte[] Records)> _snapshots = null!;
    private Action<long, ReadOnlyMemory<byte>> _onSnapshot = null!;
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
            _network, _worlds.Codec, TestProtocol.Hash, TestWorldSetups.LocalPlayer(), _owner,
            (tick, records) => _onSnapshot(tick, records));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_WithoutAWorld_OnlyTheSnapshotGoesOn_AsItsTickAndRecords()
    {
        _network.Receive(ServerPeer, TickUpdate(6, [1], [2]));
        _network.Receive(ServerPeer, Snapshot(7, [1, 2]));

        AssertThat(_snapshots.Count).IsEqual(1);
        AssertThat(_snapshots[0].Tick).IsEqual(7);
        AssertThat(_snapshots[0].Records).ContainsExactly(1, 2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_NotFromTheServer_IsDropped()
    {
        _network.Receive(OtherPeer, Snapshot(1, [1]));
        _network.Receive(OtherPeer, Rejection(JoinRejectReason.UidInUse));

        AssertThat(_snapshots).IsEmpty();
        AssertThat(_owner.Rejections).IsEmpty();
    }

    // Before and after the World alike: the World would throw on it
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_JoinRejected_ReachesTheOwner()
    {
        _network.Receive(ServerPeer, Rejection(JoinRejectReason.UidInUse));
        Connect();
        _network.Receive(ServerPeer, Rejection(JoinRejectReason.InternalError));

        AssertThat(_owner.Rejections).ContainsExactly(JoinRejectReason.UidInUse, JoinRejectReason.InternalError);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_Broken_BreaksTheConnectionOnce_AndTheRestIsDropped()
    {
        int broken = 0;
        _transport.ConnectionBroken += () => broken++;

        _network.Receive(ServerPeer, [(byte) ServerPacketKind.JoinRejected]);
        _network.Receive(ServerPeer, []);
        _network.Receive(ServerPeer, Rejection(JoinRejectReason.UidInUse));
        _network.Receive(ServerPeer, Snapshot(1, [1]));

        AssertThat(broken).IsEqual(1);
        AssertThat(_owner.Rejections).IsEmpty();
        AssertThat(_snapshots).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_SnapshotWithABrokenHeader_BreaksTheConnection()
    {
        int broken = 0;
        _transport.ConnectionBroken += () => broken++;

        _network.Receive(ServerPeer, [(byte) ServerPacketKind.Snapshot, 1]);

        AssertThat(broken).IsEqual(1);
        AssertThat(_snapshots).IsEmpty();
    }

    // The whole join: the snapshot builds the World inside the call, and the tick update right after it reaches it
    [TestCase]
    [RequireGodotRuntime]
    public void Enter_ThePacketsAfterItReachTheWorld()
    {
        World client = Connect();

        AssertThat(_owner.JoinedCount).IsEqual(1);
        AssertThat(client.Get<LocalPlayerPresentation>().Player.Uid).IsEqual(TestWorldSetups.LocalPlayerUid);
    }

    // Every later join changes the online players: the state section reaches the World
    [TestCase]
    [RequireGodotRuntime]
    public void Enter_StateSectionsReachTheWorld()
    {
        World client = Connect();

        _serverNetwork.Connect(OtherPeer);
        _serverNetwork.Receive(OtherPeer,
            _worlds.Codec.Encode(TestWorldSetups.LocalPlayer(OtherUid).ToJoinRequest(TestProtocol.Hash)));
        TransportWorlds.Tick(_server);

        AssertThat(client.Get<PlayerQuery>().GetOnlinePlayers().Select(player => player.Uid))
            .ContainsExactlyInAnyOrder(TestWorldSetups.LocalPlayerUid, OtherUid);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Enter_TickUpdateReachesTheWorld_AsItsTickAndSections()
    {
        var endpoint = new RecordingWorldEndpoint();
        _transport.Enter(endpoint);

        _network.Receive(ServerPeer, TickUpdate(9, [1], [2, 3]));

        RecordingWorldEndpoint.Call call = endpoint.Calls.Single();
        AssertThat(call.Name).IsEqual(nameof(World.IClientEndpoint.ReceiveTickUpdate));
        AssertThat(call.Tick).IsEqual(9);
        AssertThat(call.Body).ContainsExactly(1);
        AssertThat(call.Events).ContainsExactly(2, 3);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_TickUpdateWithABrokenHeader_BreaksTheConnection_WithoutReachingTheWorld()
    {
        var endpoint = new RecordingWorldEndpoint();
        _transport.Enter(endpoint);
        int broken = 0;
        _transport.ConnectionBroken += () => broken++;

        _network.Receive(ServerPeer, [(byte) ServerPacketKind.TickUpdate, 1, 0, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 1]);

        AssertThat(broken).IsEqual(1);
        AssertThat(endpoint.Calls).IsEmpty();
    }

    // The server sends one snapshot per join: a second one is a broken packet
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_SnapshotWithAWorld_BreaksTheConnection()
    {
        _transport.Enter(new RecordingWorldEndpoint());
        int broken = 0;
        _transport.ConnectionBroken += () => broken++;

        _network.Receive(ServerPeer, Snapshot(1, [1]));

        AssertThat(_snapshots).IsEmpty();
        AssertThat(broken).IsEqual(1);
    }

    // The World has applied part of the packet, and the server has moved on: the models would drift from here
    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_TheWorldCannotApplyIt_BreaksTheConnectionOnce()
    {
        var endpoint = new RecordingWorldEndpoint { ThrowOnReceive = true };
        _transport.Enter(endpoint);
        int broken = 0;
        _transport.ConnectionBroken += () => broken++;

        _network.Receive(ServerPeer, TickUpdate(1, [], [1]));
        _network.Receive(ServerPeer, TickUpdate(2, [1], []));

        AssertThat(broken).IsEqual(1);
        AssertThat(endpoint.Calls.Count).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Enter_Twice_Throws()
    {
        _transport.Enter(new RecordingWorldEndpoint());

        AssertThrown(() => _transport.Enter(new RecordingWorldEndpoint())).IsInstanceOf<InvalidOperationException>();
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
        _onSnapshot = (tick, records) =>
        {
            client = AutoFree(new World())!;
            _worlds.Init(client, TestWorldSetups.RemoteClient(localPlayerOwner: _owner), new NoConnection(),
                _transport, new WorldOrigin.FromSnapshot(tick, records));
            _transport.Enter(client);
        };

        _serverNetwork.Connect(ClientPeer);
        _network.ConnectToServer();
        TransportWorlds.Tick(_server);
        return client!;
    }

    private void RecordSnapshot(long tick, ReadOnlyMemory<byte> records) => _snapshots.Add((tick, records.ToArray()));

    private static byte[] TickUpdate(long tick, byte[] state, byte[] events)
    {
        var packet = new ArrayBufferWriter<byte>();
        ServerPackets.WriteTickUpdate(tick, state, events, packet);
        return packet.WrittenSpan.ToArray();
    }

    private static byte[] Snapshot(long tick, byte[] records)
    {
        var packet = new ArrayBufferWriter<byte>();
        ServerPackets.WriteSnapshot(tick, records, packet);
        return packet.WrittenSpan.ToArray();
    }

    private static byte[] Rejection(JoinRejectReason reason)
    {
        var packet = new ArrayBufferWriter<byte>();
        ServerPackets.WriteJoinRejected(reason, packet);
        return packet.WrittenSpan.ToArray();
    }
}
