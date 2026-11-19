using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using static GdUnit4.Assertions;
using static NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures.FakeSessionHandler;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Commands;

// Entries go in as packets through a real inbox sharing the dispatcher's registry, as in the game
[TestSuite]
public class CommandDispatcherTests
{
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;
    private const string AliceUid = "alice";
    private const int BobPeer = 3;
    private const string BobUid = "bob";
    private const int AliceSecondPeer = 4;
    private const string ThrowInProcess = "throw in process";

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;
    private EventOutbox _outbox = null!;
    private PlayersStorage _playersStorage = null!;
    private PlayersSessionStorage _sessionStorage = null!;
    private PlayersModel _playersModel = null!;
    private PlayerQuery _players = null!;
    private CommandHandlerRegistry _handlers = null!;
    private CommandInbox _inbox = null!;
    private List<string> _calls = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _peers = new PeerUidMap();
        _clientsConnection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _gatekeeper = new PeerGatekeeper(_clientsConnection, new ManualTimeProvider(0), _peers);
        _outbox = new EventOutbox(_codec, _peers);
        _playersStorage = new PlayersStorage();
        _sessionStorage = new PlayersSessionStorage();
        var registry = new EntityRegistry();
        registry.Register(new NetId(1), _playersStorage, 0);
        registry.Register(new NetId(2), _sessionStorage, 0);
        _playersModel = _playersStorage.Model;
        _players = new PlayerQuery(new PlayersStorageQuery(registry), new PlayersSessionStorageQuery(registry));
        _handlers = new CommandHandlerRegistry();
        _inbox = new CommandInbox(_codec, _gatekeeper, _handlers);
        _calls = [];
    }

    [AfterTest]
    public void TearDown()
    {
        _playersStorage.Free();
        _sessionStorage.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_CommandFromNotJoinedPeer_IsDropped()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));

        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_BoundPeers_ReachHandlerWithTheirUids()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        JoinDirectly(AliceUid, AlicePeer);
        JoinDirectly(BobUid, BobPeer);

        Chat(BobPeer, "hi");
        Chat(AlicePeer, "hello");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            "validate bob: hi", "process bob: hi", "validate alice: hello", "process alice: hello");
    }

    // Bound to a peer, but the uid has no player: a broken join or leave, which the lookup throws on
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_ThrowingLookupInHandler_DropsOnlyThatCommand()
    {
        CommandDispatcher dispatcher = Dispatcher(new LookingUpHandler(_calls, _players));
        JoinDirectly(AliceUid, AlicePeer);
        JoinDirectly(BobUid, BobPeer);
        _playersModel.AddPlayer(BobUid).Nick = "Bob";

        Chat(AlicePeer, "lost");
        Chat(BobPeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("Bob: hi");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_FailedValidate_DropsOnlyThatCommand()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        JoinDirectly(AliceUid, AlicePeer);

        Chat(AlicePeer, ThrowInValidate);
        Chat(AlicePeer, Invalid);
        Chat(AlicePeer, "next");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            $"validate alice: {ThrowInValidate}",
            $"validate alice: {Invalid}",
            "validate alice: next",
            "process alice: next");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_ThrowingProcess_DoesNotStopTheNextCommand()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        JoinDirectly(AliceUid, AlicePeer);

        Chat(AlicePeer, ThrowInProcess);
        Chat(AlicePeer, "next");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            $"validate alice: {ThrowInProcess}",
            $"process alice: {ThrowInProcess}",
            "validate alice: next",
            "process alice: next");
    }

    // The join is processed before the chat of the same tick, so the chat already has its sender
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_ChatBeforeJoinDropped_JoinThenChatInOneTickBothProcessed()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), SessionHandler());

        Chat(AlicePeer, "too early");
        Join(AlicePeer, AliceUid);
        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            "validate join alice",
            Joined(AliceUid),
            "validate alice: hi",
            "process alice: hi");
    }

    // Between the disconnect and its peer_disconnected the displaced peer still sends: it must not displace back
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_DisplacedPeer_IsIgnoredUntilItsDisconnection()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), SessionHandler());
        Join(AlicePeer, AliceUid);
        dispatcher.ProcessAll();
        _calls.Clear();

        Join(AliceSecondPeer, AliceUid);
        Join(AlicePeer, AliceUid);
        Chat(AlicePeer, "still here");
        _inbox.EnqueuePeerDisconnected(AlicePeer);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", "leave alice", Joined(AliceUid));
        AssertBound(AliceSecondPeer, AliceUid);
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_JoinedPeerDisconnected_LeavesAndLosesBindingAndBuffer()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), SessionHandler());
        Join(AlicePeer, AliceUid);
        dispatcher.ProcessAll();
        _calls.Clear();

        _inbox.EnqueuePeerDisconnected(AlicePeer);
        Chat(AlicePeer, "after leaving");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("leave alice");
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(AliceUid, out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_BeforeRegister_Throws()
    {
        CommandDispatcher dispatcher = Unregistered();

        AssertThrown(() => dispatcher.ProcessAll()).IsInstanceOf<InvalidOperationException>();
    }

    private CommandDispatcher Dispatcher(params object[] handlers)
    {
        _handlers.Register(handlers);
        return Unregistered();
    }

    private CommandDispatcher Unregistered() =>
        new(_inbox, _handlers, new PeerSessions(_handlers, _peers, _outbox, _gatekeeper), _peers, _gatekeeper);

    private FakeSessionHandler SessionHandler() => new(_calls, _peers, _outbox);

    // The dispatcher looks only at the binding: whether the player exists is the handler's lookup
    private void JoinDirectly(string uid, int peerId) => _peers.Bind(uid, peerId);

    private void Chat(int peerId, string text) =>
        _inbox.EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));

    private void Join(int peerId, string uid) =>
        _inbox.EnqueueFromPeer(
            peerId, _codec.Encode(new JoinRequestCommand(_codec.ProtocolHash, uid, uid, Colors.Red)));

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

    private class PlayerChatHandler(List<string> calls) : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(string senderUid, SendChatMessageCommand command)
        {
            calls.Add($"validate {senderUid}: {command.Text}");
            return command.Text switch
            {
                ThrowInValidate => throw new InvalidOperationException("validate failed"),
                Invalid => false,
                _ => true,
            };
        }

        public void Process(string senderUid, SendChatMessageCommand command)
        {
            calls.Add($"process {senderUid}: {command.Text}");
            if (command.Text == ThrowInProcess) throw new InvalidOperationException("process failed");
        }
    }

    private class LookingUpHandler(List<string> calls, PlayerQuery players)
        : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(string senderUid, SendChatMessageCommand command) => true;

        public void Process(string senderUid, SendChatMessageCommand command) =>
            calls.Add($"{players.Get(senderUid).Nick}: {command.Text}");
    }
}
