using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;
using static NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures.FakeSessionHandler;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Peers;

[TestSuite]
public class PeerSessionsTests
{
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private static readonly PlayerUid HostUid = new("host");
    private const int AlicePeer = 2;
    private static readonly PlayerUid AliceUid = new("alice");
    private const int BobPeer = 3;
    private const int AliceSecondPeer = 4;

    private NetMessageCodec _codec = null!;
    private PeerStateTable _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;
    private List<string> _calls = null!;
    private PeerSessions _sessions = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        _peers = new PeerStateTable();
        _clientsConnection = new RecordingClientsConnection();
        _gatekeeper = new PeerGatekeeper(_clientsConnection, new ManualTimeProvider(0), _peers);
        _calls = [];
        _sessions = new PeerSessions(
            new FakeSessionHandler(_calls, _peers), _peers, _gatekeeper, new ProtectedPlayer(HostUid));
        foreach (int peerId in (int[]) [HostPeer, AlicePeer, BobPeer, AliceSecondPeer])
        {
            _gatekeeper.StartHandshake(peerId);
        }
    }

    // The session handler publishes the join events, which the joiner must get too
    [TestCase]
    [RequireGodotRuntime]
    public void Join_JoinsThePeerBeforeJoin()
    {
        Join(AlicePeer, AliceUid);

        AssertThat(_calls).ContainsExactly("validate join alice", Joined(AliceUid));
        AssertJoined(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_RepeatedFromJoinedPeer_IsDropped()
    {
        Join(AlicePeer, AliceUid);
        Join(AlicePeer, new PlayerUid("other"));

        AssertThat(_calls).ContainsExactly("validate join alice", Joined(AliceUid));
        AssertJoined(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_FailedValidate_RejectsAndDisconnectsThePeer()
    {
        Join(AlicePeer, Invalid);
        Join(BobPeer, ThrowInValidate);

        AssertThat(_calls).ContainsExactly($"validate join {Invalid}", $"validate join {ThrowInValidate}");
        AssertNotJoined(AlicePeer);
        AssertNotJoined(BobPeer);
        // The exception text stays in the log: the peer gets only the neutral code
        AssertThat(Reasons()).ContainsExactly(
            (AlicePeer, DisconnectReason.InvalidNick), (BobPeer, DisconnectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer, BobPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThrowingJoin_RollsTheJoinBackAndRejects()
    {
        Join(AlicePeer, ThrowInJoin);

        AssertThat(_calls).ContainsExactly($"validate join {ThrowInJoin}", Joined(ThrowInJoin));
        AssertNotJoined(AlicePeer);
        AssertThat(_peers.TryGetPeerIdByUid(ThrowInJoin, out _)).IsFalse();
        AssertThat(_peers.IsCut(AlicePeer)).IsTrue();
        AssertThat(Reasons()).ContainsExactly((AlicePeer, DisconnectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithUidOfAnotherPeer_DisplacesIt()
    {
        Join(AlicePeer, AliceUid);
        _calls.Clear();

        Join(AliceSecondPeer, AliceUid);

        AssertThat(_calls).ContainsExactly("validate join alice", Displaced(AliceUid), Joined(AliceUid));
        AssertNotJoined(AlicePeer);
        AssertJoined(AliceSecondPeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(_peers.IsCut(AlicePeer)).IsTrue();
        AssertThat(Reasons()).ContainsExactly((AlicePeer, DisconnectReason.Displaced));

        _sessions.Disconnected(AlicePeer);

        AssertThat(_calls).ContainsExactly("validate join alice", Displaced(AliceUid), Joined(AliceUid));
        AssertJoined(AliceSecondPeer, AliceUid);
    }

    // Disconnected by the server, the old peer still holds its uid: its Leave is still due
    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithUidOfLeavingPeer_DisplacesItWithOneLeave()
    {
        Join(AlicePeer, AliceUid);
        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.MissedPacket);
        _calls.Clear();
        _clientsConnection.Packets.Clear();

        Join(AliceSecondPeer, AliceUid);
        _sessions.Disconnected(AlicePeer);

        AssertThat(_calls).ContainsExactly("validate join alice", Displaced(AliceUid), Joined(AliceUid));
        AssertJoined(AliceSecondPeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(Reasons()).IsEmpty();
    }

    // The uid would otherwise stay taken by a peer that never leaves
    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThrowingLeaveOfDisplacedPeer_FreesTheUid_RejectsTheNewPeer()
    {
        Join(AlicePeer, ThrowInLeave);

        Join(AliceSecondPeer, ThrowInLeave);

        AssertThat(_calls).Contains(Displaced(ThrowInLeave));
        AssertNotJoined(AlicePeer);
        AssertNotJoined(AliceSecondPeer);
        AssertThat(_peers.TryGetPeerIdByUid(ThrowInLeave, out _)).IsFalse();
        AssertThat(Reasons()).ContainsExactly(
            (AlicePeer, DisconnectReason.Displaced), (AliceSecondPeer, DisconnectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer, AliceSecondPeer);
    }

    // The owner of the server cannot reconnect without leaving it, so it is never displaced
    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithTheProtectedUid_IsRejected()
    {
        Join(HostPeer, HostUid);
        _calls.Clear();

        Join(AlicePeer, HostUid);

        AssertThat(_calls).ContainsExactly("validate join host");
        AssertJoined(HostPeer, HostUid);
        AssertNotJoined(AlicePeer);
        AssertThat(Reasons()).ContainsExactly((AlicePeer, DisconnectReason.UidInUse));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    // Until its peer_disconnected a cut peer still sends: its join must not displace the player online with that uid
    [TestCase]
    [RequireGodotRuntime]
    public void Join_FromCutPeer_IsDroppedAndDisplacesNobody()
    {
        Join(AlicePeer, AliceUid);
        Join(BobPeer, new PlayerUid("bob"));
        _gatekeeper.Disconnect(BobPeer, DisconnectReason.MissedPacket);
        _gatekeeper.Disconnect(AliceSecondPeer, DisconnectReason.MissedPacket);
        _calls.Clear();
        _clientsConnection.Packets.Clear();

        Join(BobPeer, AliceUid);
        Join(AliceSecondPeer, AliceUid);

        AssertThat(_calls).IsEmpty();
        AssertJoined(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(BobPeer, AliceSecondPeer);
        AssertThat(Reasons()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_FromPeerWithoutHandshake_IsDropped()
    {
        const int unknownPeer = 5;

        Join(unknownPeer, AliceUid);

        AssertThat(_calls).IsEmpty();
        AssertNotJoined(unknownPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_JoinedPeer_LeavesAndIsForgotten()
    {
        Join(AlicePeer, AliceUid);
        _calls.Clear();

        _sessions.Disconnected(AlicePeer);

        AssertThat(_calls).ContainsExactly("leave alice");
        AssertNotJoined(AlicePeer);
        AssertThat(_peers.TryGetPeerIdByUid(AliceUid, out _)).IsFalse();
    }

    // A peer disconnected by the server leaves in the tick of its peer_disconnected, like any other
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_LeavingPeer_LeavesOnlyThen()
    {
        Join(AlicePeer, AliceUid);
        _calls.Clear();

        _gatekeeper.Disconnect(AlicePeer, DisconnectReason.MissedPacket);

        AssertThat(_calls).IsEmpty();
        AssertThat(_peers.TryGetPeerIdByUid(AliceUid, out _)).IsTrue();

        _sessions.Disconnected(AlicePeer);

        AssertThat(_calls).ContainsExactly("leave alice");
        AssertThat(_peers.TryGetPeerIdByUid(AliceUid, out _)).IsFalse();
        AssertThat(_peers.IsCut(AlicePeer)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_NotJoinedPeer_CallsNoHandlerAndIsForgotten()
    {
        Join(AlicePeer, Invalid);
        _calls.Clear();

        _sessions.Disconnected(AlicePeer);
        _sessions.Disconnected(BobPeer);

        AssertThat(_calls).IsEmpty();
        AssertThat(_peers.IsCut(AlicePeer)).IsFalse();
    }

    // A peer left in the table would block its uid until the server restarts
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_ThrowingLeave_StillForgetsThePeer()
    {
        Join(AlicePeer, ThrowInLeave);

        AssertThrown(() => _sessions.Disconnected(AlicePeer)).IsInstanceOf<InvalidOperationException>();

        AssertThat(_calls).Contains($"leave {ThrowInLeave}");
        AssertNotJoined(AlicePeer);
        AssertThat(_peers.TryGetPeerIdByUid(ThrowInLeave, out _)).IsFalse();
    }

    private void Join(int peerId, PlayerUid uid) => _sessions.Join(peerId, Command(uid));

    private JoinRequestCommand Command(PlayerUid uid) => new(TestProtocol.Hash.Value, uid, uid.Value, Colors.Red);

    private void AssertJoined(int peerId, PlayerUid uid)
    {
        AssertThat(_peers.TryGetJoined(peerId, out PeerStateTable.Joined? joined)).IsTrue();
        AssertThat(joined!.Uid).IsEqual(uid);
    }

    private void AssertNotJoined(int peerId) => AssertThat(_peers.IsJoined(peerId)).IsFalse();

    private List<(int, DisconnectReason)> Reasons() =>
        _clientsConnection.Packets
            .Where(sent => sent.Kind == SentKind.DisconnectReason)
            .Select(sent => (sent.PeerId, (DisconnectReason) sent.Body[0]))
            .ToList();
}
