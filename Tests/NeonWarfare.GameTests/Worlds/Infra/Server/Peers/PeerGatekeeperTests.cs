using GdUnit4;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Peers;

[TestSuite]
public class PeerGatekeeperTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private ManualTimeProvider _time = null!;
    private PeerStateTable _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;

    [BeforeTest]
    public void SetUp()
    {
        _time = new ManualTimeProvider(1_700_000_000);
        _peers = new PeerStateTable();
        _clientsConnection = new RecordingClientsConnection();
        _gatekeeper = new PeerGatekeeper(_clientsConnection, _time, _peers);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectExpired_PeerNotJoinedByTheDeadline_IsDisconnectedWithTheTimeoutReason()
    {
        _gatekeeper.StartHandshake(AlicePeer);
        _time.Now += PeerGatekeeper.HandshakeTimeout - TimeSpan.FromMilliseconds(1);
        _gatekeeper.StartHandshake(BobPeer);

        _gatekeeper.DisconnectExpired();
        AssertThat(_clientsConnection.Disconnected).IsEmpty();

        _time.Now += TimeSpan.FromMilliseconds(1);
        _gatekeeper.DisconnectExpired();
        _gatekeeper.DisconnectExpired();

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(_clientsConnection.Packets.Select(sent => (sent.PeerId, sent.Kind, sent.Body[0])))
            .ContainsExactly((AlicePeer, SentKind.DisconnectReason, (byte) DisconnectReason.HandshakeTimeout));
        AssertThat(_peers.IsCut(AlicePeer)).IsTrue();
        AssertThat(_peers.IsCut(BobPeer)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectExpired_JoinedPeer_StaysAfterTheDeadline()
    {
        _gatekeeper.StartHandshake(AlicePeer);
        _peers.Join(AlicePeer, new PlayerUid("alice"));
        _time.Now += PeerGatekeeper.HandshakeTimeout;

        _gatekeeper.DisconnectExpired();

        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    // The client reads the reason only if it comes before the disconnect
    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectWithReason_SendsTheReasonThenDisconnects()
    {
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.InvalidNick);
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.InvalidColor);

        AssertThat(_clientsConnection.Packets.Count).IsEqual(1);
        AssertThat(_clientsConnection.Packets[0].PeerId).IsEqual(AlicePeer);
        AssertThat(_clientsConnection.Packets[0].Kind).IsEqual(SentKind.DisconnectReason);
        AssertThat(_clientsConnection.Packets[0].Body).ContainsExactly((byte) DisconnectReason.InvalidNick);
        AssertThat(_clientsConnection.Timeline.Select(entry => entry.GetType().Name))
            .ContainsExactly(nameof(RecordingClientsConnection.Sent), nameof(RecordingClientsConnection.Disconnection));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectWithReason_FailedSend_StillDisconnects()
    {
        _clientsConnection.FailingPeer = AlicePeer;

        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.InvalidNick);

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_Twice_CallsTheTransportOnce()
    {
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.MissedPacket);
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.MissedPacket);
        _gatekeeper.StartHandshake(BobPeer);
        _peers.Join(BobPeer, new PlayerUid("bob"));
        _gatekeeper.Disconnect(BobPeer, DisconnectReason.MissedPacket);
        _gatekeeper.Disconnect(BobPeer, DisconnectReason.MissedPacket);

        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer, BobPeer);
        AssertThat(_peers.IsCut(BobPeer)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DisconnectWithReason_CutPeer_SendsNothing()
    {
        _gatekeeper.StartHandshake(AlicePeer);
        _peers.Join(AlicePeer, new PlayerUid("alice"));
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.MissedPacket);
        _clientsConnection.Packets.Clear();

        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.InternalError);

        AssertThat(_clientsConnection.Packets).IsEmpty();
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }
}
