using GdUnit4;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork;

[TestSuite]
public class PeerGatekeeperTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private ManualTimeProvider _time = null!;
    private PeerUidMap _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;

    [BeforeTest]
    public void SetUp()
    {
        _time = new ManualTimeProvider(1_700_000_000);
        _peers = new PeerUidMap();
        _clientsConnection = new RecordingClientsConnection();
        _gatekeeper = new PeerGatekeeper(_clientsConnection, _time, _peers);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectExpired_PeerNotJoinedByTheDeadline_IsDisconnected()
    {
        _gatekeeper.OnPeerConnected(AlicePeer);
        _time.Now += PeerGatekeeper.HandshakeTimeout - TimeSpan.FromMilliseconds(1);
        _gatekeeper.OnPeerConnected(BobPeer);

        _gatekeeper.DisconnectExpired();
        AssertThat(_clientsConnection.Disconnected).IsEmpty();

        _time.Now += TimeSpan.FromMilliseconds(1);
        _gatekeeper.DisconnectExpired();
        _gatekeeper.DisconnectExpired();

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsTrue();
        AssertThat(_gatekeeper.IsDisconnecting(BobPeer)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectExpired_JoinedPeer_StaysAfterTheDeadline()
    {
        _gatekeeper.OnPeerConnected(AlicePeer);
        _peers.Bind("alice", AlicePeer);
        _time.Now += PeerGatekeeper.HandshakeTimeout;

        _gatekeeper.DisconnectExpired();

        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    // The client reads the reason only if it comes before the disconnect
    [TestCase]
    [RequireGodotRuntime]
    public void Reject_SendsTheReasonThenDisconnects()
    {
        _gatekeeper.Reject(AlicePeer, JoinRejectReason.InvalidNick);
        _gatekeeper.Reject(AlicePeer, JoinRejectReason.InvalidColor);

        AssertThat(_clientsConnection.Packets.Count).IsEqual(1);
        AssertThat(_clientsConnection.Packets[0].PeerId).IsEqual(AlicePeer);
        AssertThat(_clientsConnection.Packets[0].Packet)
            .ContainsExactly((byte) ServerPacketKind.JoinRejected, (byte) JoinRejectReason.InvalidNick);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Reject_FailedSend_StillDisconnects()
    {
        _clientsConnection.FailingPeer = AlicePeer;

        AssertThrown(() => _gatekeeper.Reject(AlicePeer, JoinRejectReason.InvalidNick))
            .IsInstanceOf<InvalidOperationException>();

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_Twice_CallsTheTransportOnce()
    {
        _gatekeeper.Disconnect(AlicePeer);
        _gatekeeper.Disconnect(AlicePeer);

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    // ENet may give a later peer the same id
    [TestCase]
    [RequireGodotRuntime]
    public void Forget_ClearsTheMarkAndTheDeadline()
    {
        _gatekeeper.OnPeerConnected(BobPeer);
        _gatekeeper.Disconnect(AlicePeer);

        _gatekeeper.Forget(AlicePeer);
        _gatekeeper.Forget(BobPeer);
        _time.Now += PeerGatekeeper.HandshakeTimeout;
        _gatekeeper.DisconnectExpired();

        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsFalse();
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsLocal_OnlyTheHostsPeer()
    {
        AssertThat(_gatekeeper.IsLocal(RecordingClientsConnection.HostPeer)).IsFalse();

        _clientsConnection.LocalPeerId = RecordingClientsConnection.HostPeer;

        AssertThat(_gatekeeper.IsLocal(RecordingClientsConnection.HostPeer)).IsTrue();
        AssertThat(_gatekeeper.IsLocal(AlicePeer)).IsFalse();
    }
}
