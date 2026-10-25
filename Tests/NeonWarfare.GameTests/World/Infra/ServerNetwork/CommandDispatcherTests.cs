using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Features.Storages;
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

    private const int AlicePeer = 2;
    private const string AliceUid = "alice";
    private const int BobPeer = 3;
    private const string BobUid = "bob";

    private const string Invalid = "invalid";
    private const string ThrowInValidate = "throw in validate";
    private const string ThrowInProcess = "throw in process";

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private PersistenceStorage _persistenceStorage = null!;
    private SessionStorage _sessionStorage = null!;
    private PersistenceModel _persistence = null!;
    private PlayerQuery _players = null!;
    private CommandInbox _inbox = null!;
    private List<string> _calls = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _peers = new PeerUidMap();
        _persistenceStorage = new PersistenceStorage();
        _sessionStorage = new SessionStorage();
        var registry = new EntityRegistry();
        registry.Register(new NetId(1), _persistenceStorage);
        registry.Register(new NetId(2), _sessionStorage);
        _persistence = _persistenceStorage.Model;
        _players = new PlayerQuery(new PersistenceStorageQuery(registry), new SessionStorageQuery(registry));
        _inbox = new CommandInbox(_codec);
        _calls = [];
    }

    [AfterTest]
    public void TearDown()
    {
        _persistenceStorage.Free();
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
        _persistence.AddPlayer(BobUid).Nick = "Bob";

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
            "process join alice",
            "validate alice: hi",
            "process alice: hi");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_RepeatedJoinFromJoinedPeer_IsDropped()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, AliceUid);
        Join(AlicePeer, "other");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate join alice", "process join alice");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_FailedJoinValidate_DoesNotProcessTheJoin()
    {
        CommandDispatcher dispatcher = Dispatcher(JoinHandler());

        Join(AlicePeer, Invalid);
        Join(BobPeer, ThrowInValidate);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly($"validate join {Invalid}", $"validate join {ThrowInValidate}");
        AssertThat(_peers.TryGetUid(AlicePeer, out _)).IsFalse();
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
                 })
        {
            var dispatcher = new CommandDispatcher(_inbox, _peers);

            AssertThrown(() => dispatcher.Register(handlers)).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_ObjectImplementingNoHandler_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers);

        AssertThrown(() => dispatcher.Register([new object()])).IsInstanceOf<InvalidOperationException>();
    }

    // ProcessAll routes every join to the join handler: a player handler of it would silently never run
    [TestCase]
    [RequireGodotRuntime]
    public void Register_PlayerHandlerOfJoinRequest_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers);

        AssertThrown(() => dispatcher.Register([new PlayerJoinHandler()])).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_BeforeRegister_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers);

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

        AssertThat(whitelist).Contains(typeof(SendChatMessageCommand));
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
        var dispatcher = new CommandDispatcher(_inbox, _peers);
        dispatcher.Register(handlers);
        return dispatcher;
    }

    private FakeJoinHandler JoinHandler() => new(_calls, _peers);

    // The dispatcher looks only at the binding: whether the player exists is the handler's lookup
    private void JoinDirectly(string uid, int peerId) => _peers.Bind(uid, peerId);

    private void Chat(int peerId, string text) =>
        _inbox.EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));

    private void Join(int peerId, string uid) =>
        _inbox.EnqueueFromPeer(peerId, _codec.Encode(new JoinRequestCommand(1, uid, uid, Colors.Red)));

    private WorldDependencies Dependencies() =>
        new(TimeProvider.System, _codec, new ManualFrameProvider(), AutoFree(TestWorldScenes.Create())!,
            new RecordingClientsConnection());

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

    // Stands for the join of task 015: binds the peer, which is all the dispatcher looks at
    private class FakeJoinHandler(List<string> calls, PeerUidMap peers)
        : IJoinRequestHandler
    {
        public bool Validate(int peerId, JoinRequestCommand command, out string reason)
        {
            calls.Add($"validate join {command.Uid}");
            reason = "rejected";
            return command.Uid switch
            {
                ThrowInValidate => throw new InvalidOperationException("validate failed"),
                Invalid => false,
                _ => true,
            };
        }

        public void Process(int peerId, JoinRequestCommand command)
        {
            calls.Add($"process join {command.Uid}");
            peers.Bind(command.Uid, peerId);
        }
    }
}
