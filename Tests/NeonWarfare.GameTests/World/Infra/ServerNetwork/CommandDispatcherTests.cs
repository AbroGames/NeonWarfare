using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork;

// Entries go in as packets through a real inbox registered with the dispatcher's whitelist, as in the game
[TestSuite]
public class CommandDispatcherTests
{
    private const WorldLayer Host = WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler
                                    | WorldLayer.ServerNetwork | WorldLayer.Query | WorldLayer.Presentation
                                    | WorldLayer.ClientNetwork;

    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const string HostUid = "host";
    private const int AlicePeer = 2;
    private const string AliceUid = "alice";
    private const int BobPeer = 3;
    private const string BobUid = "bob";
    private const int AliceSecondPeer = 4;

    private const string Invalid = "invalid";
    private const string ThrowInValidate = "throw in validate";
    private const string ThrowInProcess = "throw in process";
    private const string ThrowInLeave = "throw in leave";
    private const string ProcessedJoin = "process join alice: bound, buffer";

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;
    private EventOutbox _outbox = null!;
    private PlayersStorage _playersStorage = null!;
    private PlayersSessionStorage _sessionStorage = null!;
    private PlayersModel _playersModel = null!;
    private PlayerQuery _players = null!;
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
        registry.Register(new NetId(1), _playersStorage);
        registry.Register(new NetId(2), _sessionStorage);
        _playersModel = _playersStorage.Model;
        _players = new PlayerQuery(new PlayersStorageQuery(registry), new PlayersSessionStorageQuery(registry));
        _inbox = new CommandInbox(_codec, _gatekeeper);
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
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), JoinHandler());

        Chat(AlicePeer, "too early");
        Join(AlicePeer, AliceUid);
        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            "validate join alice",
            ProcessedJoin,
            "validate alice: hi",
            "process alice: hi");
    }

    // The join handler publishes the join events, which the joiner must get too
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_Join_BindsThePeerAndCreatesItsBufferBeforeProcess()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, AliceUid);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", ProcessedJoin);
        AssertBound(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_RepeatedJoinFromJoinedPeer_IsDropped()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, AliceUid);
        Join(AlicePeer, "other");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", ProcessedJoin);
        AssertBound(AlicePeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_FailedJoinValidate_RejectsAndDisconnectsThePeer()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, Invalid);
        Join(BobPeer, ThrowInValidate);
        dispatcher.ProcessAll();

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
    public void ProcessAll_ThrowingJoinProcess_RollsTheJoinBackAndRejects()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, ThrowInProcess);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            $"validate join {ThrowInProcess}", $"process join {ThrowInProcess}: bound, buffer");
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(ThrowInProcess, out _)).IsFalse();
        AssertThat(Rejections()).ContainsExactly((AlicePeer, JoinRejectReason.InternalError));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_JoinWithUidOfAnotherPeer_DisplacesIt()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());
        Join(AlicePeer, AliceUid);
        dispatcher.ProcessAll();
        _calls.Clear();

        Join(AliceSecondPeer, AliceUid);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", "leave alice", ProcessedJoin);
        AssertNotBound(AlicePeer);
        AssertBound(AliceSecondPeer, AliceUid);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(Rejections()).IsEmpty();
    }

    // Unbound and without a deadline, the old peer would otherwise stay connected until it leaves on its own
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_ThrowingLeaveOfDisplacedPeer_StillDisconnectsIt()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());
        Join(AlicePeer, ThrowInLeave);
        dispatcher.ProcessAll();

        Join(AliceSecondPeer, ThrowInLeave);
        dispatcher.ProcessAll();

        AssertThat(_calls).Contains($"leave {ThrowInLeave}");
        AssertNotBound(AlicePeer);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    // Between the disconnect and its peer_disconnected the displaced peer still sends: it must not displace back
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_DisplacedPeer_IsIgnoredUntilItsDisconnection()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), JoinHandler());
        Join(AlicePeer, AliceUid);
        dispatcher.ProcessAll();
        _calls.Clear();

        Join(AliceSecondPeer, AliceUid);
        Join(AlicePeer, AliceUid);
        Chat(AlicePeer, "still here");
        _inbox.EnqueuePeerDisconnected(AlicePeer);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", "leave alice", ProcessedJoin);
        AssertBound(AliceSecondPeer, AliceUid);
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsFalse();
    }

    // The host cannot reconnect from another peer, so it is never displaced
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_JoinWithTheHostsUid_IsRejected()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());
        Join(HostPeer, HostUid);
        dispatcher.ProcessAll();
        _calls.Clear();

        Join(AlicePeer, HostUid);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join host");
        AssertBound(HostPeer, HostUid);
        AssertNotBound(AlicePeer);
        AssertThat(Rejections()).ContainsExactly((AlicePeer, JoinRejectReason.UidInUse));
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_JoinedPeerDisconnected_LeavesAndLosesBindingAndBuffer()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), JoinHandler());
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
    public void ProcessAll_NotJoinedPeerDisconnected_CallsNoHandler()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());
        Join(AlicePeer, Invalid);
        dispatcher.ProcessAll();
        _calls.Clear();

        _inbox.EnqueuePeerDisconnected(AlicePeer);
        _inbox.EnqueuePeerDisconnected(BobPeer);
        dispatcher.ProcessAll();

        AssertThat(_calls).IsEmpty();
        AssertThat(_gatekeeper.IsDisconnecting(AlicePeer)).IsFalse();
    }

    // A peer left bound would block its uid until the server restarts
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_ThrowingLeave_StillUnbindsThePeer()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());
        Join(AlicePeer, ThrowInLeave);
        dispatcher.ProcessAll();

        _inbox.EnqueuePeerDisconnected(AlicePeer);
        dispatcher.ProcessAll();

        AssertThat(_calls).Contains($"leave {ThrowInLeave}");
        AssertNotBound(AlicePeer);
        AssertThat(_peers.TryGetPeerId(ThrowInLeave, out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetworkCommandTypes_HaveJoinOnlyWithJoinHandler()
    {
        AssertThat(Dispatcher(new PlayerChatHandler(_calls)).NetworkCommandTypes)
            .ContainsExactlyInAnyOrder(typeof(SendChatMessageCommand));
        AssertThat(NewDispatcher(new PlayerChatHandler(_calls), JoinHandler()).NetworkCommandTypes)
            .ContainsExactlyInAnyOrder(typeof(SendChatMessageCommand), typeof(JoinRequestCommand));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_Twice_Throws()
    {
        CommandDispatcher dispatcher = NewDispatcher(new PlayerChatHandler(_calls));

        AssertThrown(() => dispatcher.Register([])).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_SecondHandlerOfOneCommand_Throws()
    {
        foreach (object[] handlers in new[]
                 {
                     new object[] { new PlayerChatHandler(_calls), new PlayerChatHandler(_calls) },
                     [JoinHandler(), JoinHandler()],
                     [JoinHandler(), new LeaveOnlyHandler()],
                 })
        {
            CommandDispatcher dispatcher = Unregistered();

            AssertThrown(() => dispatcher.Register(handlers)).IsInstanceOf<InvalidOperationException>();
        }
    }

    // A joined peer could never leave, or a leave handler would wait for joins that never come
    [TestCase]
    [RequireGodotRuntime]
    public void Register_JoinAndLeaveHandlersNotInPair_Throws()
    {
        foreach (object handler in new object[] { new JoinOnlyHandler(), new LeaveOnlyHandler() })
        {
            CommandDispatcher dispatcher = Unregistered();

            AssertThrown(() => dispatcher.Register([handler])).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_ObjectImplementingNoHandler_Throws()
    {
        CommandDispatcher dispatcher = Unregistered();

        AssertThrown(() => dispatcher.Register([new object()])).IsInstanceOf<InvalidOperationException>();
    }

    // ProcessAll routes every join to the join handler: a player handler of it would silently never run
    [TestCase]
    [RequireGodotRuntime]
    public void Register_PlayerHandlerOfJoinRequest_Throws()
    {
        CommandDispatcher dispatcher = Unregistered();

        AssertThrown(() => dispatcher.Register([new PlayerJoinHandler()])).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_BeforeRegister_Throws()
    {
        CommandDispatcher dispatcher = Unregistered();

        AssertThrown(() => dispatcher.ProcessAll()).IsInstanceOf<InvalidOperationException>();
    }

    // The real game types through the composition root: the whitelist is built from the handlers it found and
    // really reaches the inbox
    [TestCase]
    [RequireGodotRuntime]
    public void Build_Host_WhitelistHasOnlyCommandsAndReachesTheInbox()
    {
        using ServiceProvider provider = new WorldServicesBuilder().Build(
            Host, Dependencies(), new WorldRoot(AutoFree(new Node())!));
        IReadOnlySet<Type> whitelist = provider.GetRequiredService<CommandDispatcher>().NetworkCommandTypes;
        var inbox = provider.GetRequiredService<CommandInbox>();
        var chat = new SendChatMessageCommand("hi");

        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(new ChatServerMessageEvent(1, "an event")));
        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(chat));

        AssertThat(whitelist).Contains(typeof(SendChatMessageCommand), typeof(JoinRequestCommand));
        AssertThat(whitelist.Where(type => !type.IsSubclassOf(typeof(Command)))).IsEmpty();
        AssertThat(whitelist.Contains(typeof(CommandInbox.PeerDisconnected))).IsFalse();
        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommand(AlicePeer, chat));
    }

    private CommandDispatcher Dispatcher(params object[] handlers)
    {
        CommandDispatcher dispatcher = NewDispatcher(handlers);
        _inbox.Register(dispatcher.NetworkCommandTypes);
        return dispatcher;
    }

    private CommandDispatcher NewDispatcher(params object[] handlers)
    {
        CommandDispatcher dispatcher = Unregistered();
        dispatcher.Register(handlers);
        return dispatcher;
    }

    private CommandDispatcher Unregistered() => new(_inbox, _peers, _outbox, _gatekeeper);

    private FakeJoinHandler JoinHandler() => new(_calls, _peers, _outbox);

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

    private List<(int, JoinRejectReason)> Rejections() =>
        _clientsConnection.Packets
            .Where(sent => sent.Packet[0] == (byte) ServerPacketKind.JoinRejected)
            .Select(sent => (sent.PeerId, (JoinRejectReason) sent.Packet[1]))
            .ToList();

    private WorldDependencies Dependencies()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new(TimeProvider.System, _codec, new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection());
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

    private class PlayerJoinHandler : IPlayerCommandHandler<JoinRequestCommand>
    {
        public bool Validate(string senderUid, JoinRequestCommand command) => true;

        public void Process(string senderUid, JoinRequestCommand command) { }
    }

    // Records whether the dispatcher has bound the peer and created its buffer by the time of Process
    private class FakeJoinHandler(List<string> calls, PeerUidMap peers, EventOutbox outbox)
        : IJoinRequestHandler, IPeerDisconnectedHandler
    {
        public bool Validate(JoinRequestCommand command, out JoinRejectReason reason)
        {
            calls.Add($"validate join {command.Uid}");
            reason = JoinRejectReason.InvalidNick;
            return command.Uid switch
            {
                ThrowInValidate => throw new InvalidOperationException("validate failed"),
                Invalid => false,
                _ => true,
            };
        }

        public void Process(JoinRequestCommand command)
        {
            bool bound = peers.TryGetPeerId(command.Uid, out int peerId);
            string buffer = bound && outbox.Peers.Contains(peerId) ? "buffer" : "no buffer";
            calls.Add($"process join {command.Uid}: {(bound ? "bound" : "not bound")}, {buffer}");
            if (command.Uid == ThrowInProcess) throw new InvalidOperationException("process failed");
        }

        public void Process(string uid)
        {
            calls.Add($"leave {uid}");
            if (uid == ThrowInLeave) throw new InvalidOperationException("leave failed");
        }
    }

    private class JoinOnlyHandler : IJoinRequestHandler
    {
        public bool Validate(JoinRequestCommand command, out JoinRejectReason reason)
        {
            reason = default;
            return true;
        }

        public void Process(JoinRequestCommand command) { }
    }

    private class LeaveOnlyHandler : IPeerDisconnectedHandler
    {
        public void Process(string uid) { }
    }
}
