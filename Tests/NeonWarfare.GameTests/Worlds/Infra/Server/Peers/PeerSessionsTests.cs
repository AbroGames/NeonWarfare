using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using static GdUnit4.Assertions;
using static NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures.FakeSessionHandler;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Peers;

[TestSuite]
public class PeerSessionsTests
{
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const string HostUid = "host";
    private const int AlicePeer = 2;
    private const string AliceUid = "alice";
    private const int BobPeer = 3;
    private const int AliceSecondPeer = 4;

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;
    private EventOutbox _outbox = null!;
    private List<string> _calls = null!;
    private PeerSessions _sessions = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _peers = new PeerUidMap();
        _clientsConnection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _gatekeeper = new PeerGatekeeper(_clientsConnection, new ManualTimeProvider(0), _peers);
        _outbox = new EventOutbox(_codec, _peers);
        _calls = [];
        var handlers = new CommandHandlerRegistry();
        handlers.Register([new FakeSessionHandler(_calls, _peers, _outbox)]);
        _sessions = new PeerSessions(handlers, _peers, _outbox, _gatekeeper);
    }

    // The session handler publishes the join events, which the joiner must get too
    [TestCase]
    [RequireGodotRuntime]
    public void Join_BindsThePeerAndCreatesItsBufferBeforeJoin()
    {
        Join(AlicePeer, AliceUid);

        AssertThat(_calls).ContainsExactly("validate join alice", Joined(AliceUid));
        AssertBound(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_RepeatedFromJoinedPeer_IsDropped()
    {
        Join(AlicePeer, AliceUid);
        Join(AlicePeer, "other");

        AssertThat(_calls).ContainsExactly("validate join alice", Joined(AliceUid));
        AssertBound(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_FailedValidate_RejectsAndDisconnectsThePeer()
    {
        Join(AlicePeer, Invalid);
        Join(BobPeer, ThrowInValidate);

        AssertThat(_calls).ContainsExactly($"validate join {Invalid}", $"validate join {ThrowInValidate}");
        AssertNotBound(AlicePeer);
        AssertNotBound(BobPeer);
        // The exception text stays in the log: the peer gets only the neutral code
        AssertThat(Rejections()).ContainsExactly(
            (AlicePeer, JoinRejectReason.InvalidNick), (BobPeer, JoinRejectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer, BobPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThrowingJoin_RollsTheJoinBackAndRejects()
    {
        Join(AlicePeer, ThrowInJoin);

        AssertThat(_calls).ContainsExactly($"validate join {ThrowInJoin}", Joined(ThrowInJoin));
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(ThrowInJoin, out _)).IsFalse();
        AssertThat(Rejections()).ContainsExactly((AlicePeer, JoinRejectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithUidOfAnotherPeer_DisplacesIt()
    {
        Join(AlicePeer, AliceUid);
        _calls.Clear();

        Join(AliceSecondPeer, AliceUid);

        AssertThat(_calls).ContainsExactly("validate join alice", "leave alice", Joined(AliceUid));
        AssertNotBound(AlicePeer);
        AssertBound(AliceSecondPeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsTrue();
        AssertThat(Rejections()).IsEmpty();
    }

    // Unbound and without a deadline, the old peer would otherwise stay connected until it leaves on its own
    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThrowingLeaveOfDisplacedPeer_StillDisconnectsIt()
    {
        Join(AlicePeer, ThrowInLeave);

        AssertThrown(() => Join(AliceSecondPeer, ThrowInLeave)).IsInstanceOf<InvalidOperationException>();

        AssertThat(_calls).Contains($"leave {ThrowInLeave}");
        AssertNotBound(AlicePeer);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    // The host cannot reconnect from another peer, so it is never displaced
    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithTheHostsUid_IsRejected()
    {
        Join(HostPeer, HostUid);
        _calls.Clear();

        Join(AlicePeer, HostUid);

        AssertThat(_calls).ContainsExactly("validate join host");
        AssertBound(HostPeer, HostUid);
        AssertNotBound(AlicePeer);
        AssertThat(Rejections()).ContainsExactly((AlicePeer, JoinRejectReason.UidInUse));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_JoinedPeer_LeavesAndLosesBindingAndBuffer()
    {
        Join(AlicePeer, AliceUid);
        _calls.Clear();

        _sessions.Disconnected(AlicePeer);

        AssertThat(_calls).ContainsExactly("leave alice");
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(AliceUid, out _)).IsFalse();
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
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsFalse();
    }

    // A peer left bound would block its uid until the server restarts
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnected_ThrowingLeave_StillUnbindsThePeer()
    {
        Join(AlicePeer, ThrowInLeave);

        AssertThrown(() => _sessions.Disconnected(AlicePeer)).IsInstanceOf<InvalidOperationException>();

        AssertThat(_calls).Contains($"leave {ThrowInLeave}");
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(ThrowInLeave, out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_WithoutSessionHandler_Throws()
    {
        var handlers = new CommandHandlerRegistry();
        handlers.Register([]);
        var sessions = new PeerSessions(handlers, _peers, _outbox, _gatekeeper);

        AssertThrown(() => sessions.Join(AlicePeer, Command(AliceUid))).IsInstanceOf<InvalidOperationException>();
    }

    private void Join(int peerId, string uid) => _sessions.Join(peerId, Command(uid));

    private JoinRequestCommand Command(string uid) => new(_codec.ProtocolHash, uid, uid, Colors.Red);

    private void AssertBound(int peerId, string uid)
    {
        AssertThat(_peers.TryGetUid(peerId, out string? bound)).IsTrue();
        AssertThat(bound).IsEqual(uid);
        AssertThat(_outbox.Peers).Contains(peerId);
    }

    private void AssertNotBound(int peerId)
    {
        AssertThat(_peers.TryGetUid(peerId, out _)).IsFalse();
        AssertThat(_outbox.Peers.Contains(peerId)).IsFalse();
    }

    private List<(int, JoinRejectReason)> Rejections() =>
        _clientsConnection.Packets
            .Where(sent => sent.Kind == SentKind.JoinRejected)
            .Select(sent => (sent.PeerId, (JoinRejectReason) sent.Body[0]))
            .ToList();
}
