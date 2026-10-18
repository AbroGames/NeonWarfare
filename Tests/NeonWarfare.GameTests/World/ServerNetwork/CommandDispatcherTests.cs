using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.CommandHandlers;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.ServerNetwork;

// Entries go in as packets through a real inbox registered with the dispatcher's whitelist, as in the game
[TestSuite]
public class CommandDispatcherTests
{
    private const WorldLayer Host = WorldLayer.Simulation | WorldLayer.CommandHandler | WorldLayer.ServerNetwork
                                    | WorldLayer.Query | WorldLayer.Presentation
                                    | WorldLayer.ServerHudPresentation | WorldLayer.ClientNetwork;

    private const int AlicePeer = 2;
    private const string AliceUid = "alice";

    private const string Invalid = "invalid";
    private const string ThrowInValidate = "throw in validate";
    private const string ThrowInProcess = "throw in process";

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private PersistenceModel _persistence = null!;
    private CommandInbox _inbox = null!;
    private List<string> _calls = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        _peers = new PeerUidMap();
        _persistence = new PersistenceModel();
        _inbox = new CommandInbox(_codec);
        _calls = [];
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

    // Bound to a peer, but the uid has no PlayerModel: the handler would get no sender
    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_PeerWithoutPlayerModel_IsDropped()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        _peers.Bind(AliceUid, AlicePeer);

        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_JoinedPlayer_ValidatesThenProcesses()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        JoinDirectly(AliceUid, AlicePeer);

        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate alice: hi", "process alice: hi");
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
        Join(3, ThrowInValidate);
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly($"validate join {Invalid}", $"validate join {ThrowInValidate}");
        AssertThat(_peers.TryGetUid(AlicePeer, out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_DedicatedWindowCommand_ReachesOnlyItsHandler()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls), new WindowChatHandler(_calls));

        _inbox.EnqueueFromDedicatedWindow(new SendChatMessageCommand("hi"));
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate window: hi", "process window: hi");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_FailedDedicatedWindowValidate_DropsOnlyThatCommand()
    {
        CommandDispatcher dispatcher = Dispatcher(new WindowChatHandler(_calls));

        _inbox.EnqueueFromDedicatedWindow(new SendChatMessageCommand(ThrowInValidate));
        _inbox.EnqueueFromDedicatedWindow(new SendChatMessageCommand(Invalid));
        _inbox.EnqueueFromDedicatedWindow(new SendChatMessageCommand("next"));
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly(
            $"validate window: {ThrowInValidate}",
            $"validate window: {Invalid}",
            "validate window: next",
            "process window: next");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_DedicatedWindowCommandWithoutHandler_IsDropped()
    {
        CommandDispatcher dispatcher = Dispatcher(new PlayerChatHandler(_calls));
        JoinDirectly(AliceUid, AlicePeer);

        _inbox.EnqueueFromDedicatedWindow(new SendChatMessageCommand("dropped"));
        Chat(AlicePeer, "hi");
        dispatcher.ProcessAll();

        AssertThat(_calls).ContainsExactly("validate alice: hi", "process alice: hi");
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
    public void Register_SecondHandlerOfOneCommandAndSender_Throws()
    {
        foreach (object[] handlers in new[]
                 {
                     new object[] { new PlayerChatHandler(_calls), new PlayerChatHandler(_calls) },
                     [new WindowChatHandler(_calls), new WindowChatHandler(_calls)],
                     [JoinHandler(), JoinHandler()],
                 })
        {
            var dispatcher = new CommandDispatcher(_inbox, _peers, _persistence);

            AssertThrown(() => dispatcher.Register(handlers)).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_ObjectImplementingNoHandler_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers, _persistence);

        AssertThrown(() => dispatcher.Register([new object()])).IsInstanceOf<InvalidOperationException>();
    }

    // ProcessAll routes every join to the join handler: a player handler of it would silently never run
    [TestCase]
    [RequireGodotRuntime]
    public void Register_PlayerHandlerOfJoinRequest_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers, _persistence);

        AssertThrown(() => dispatcher.Register([new PlayerJoinHandler()])).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ProcessAll_BeforeRegister_Throws()
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers, _persistence);

        AssertThrown(() => dispatcher.ProcessAll()).IsInstanceOf<InvalidOperationException>();
    }

    // The real game types through the composition root: the whitelist is built from the handlers it found and
    // really reaches the inbox
    [TestCase]
    [RequireGodotRuntime]
    public void Build_Host_WhitelistHasOnlyCommandsAndReachesTheInbox()
    {
        using ServiceProvider provider = new WorldServicesBuilder().Build(Host, Dependencies());
        IReadOnlySet<Type> whitelist = provider.GetRequiredService<CommandDispatcher>().NetworkCommandTypes;
        var inbox = provider.GetRequiredService<CommandInbox>();
        var chat = new SendChatMessageCommand("hi");

        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(new ChatServerMessageEvent(1, "an event")));
        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(chat));

        AssertThat(whitelist).Contains(typeof(SendChatMessageCommand));
        AssertThat(whitelist.Where(type => !type.IsSubclassOf(typeof(Command)))).IsEmpty();
        AssertThat(whitelist.Contains(typeof(CommandInbox.PeerDisconnected))).IsFalse();
        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.FromPeer(AlicePeer, chat));
    }

    private CommandDispatcher Dispatcher(params object[] handlers)
    {
        CommandDispatcher dispatcher = NewDispatcher(handlers);
        _inbox.Register(dispatcher.NetworkCommandTypes);
        return dispatcher;
    }

    private CommandDispatcher NewDispatcher(params object[] handlers)
    {
        var dispatcher = new CommandDispatcher(_inbox, _peers, _persistence);
        dispatcher.Register(handlers);
        return dispatcher;
    }

    private FakeJoinHandler JoinHandler() => new(_calls, _peers, _persistence);

    private void JoinDirectly(string uid, int peerId)
    {
        _persistence.AddPlayer(uid);
        _peers.Bind(uid, peerId);
    }

    private void Chat(int peerId, string text) =>
        _inbox.EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));

    private void Join(int peerId, string uid) =>
        _inbox.EnqueueFromPeer(peerId, _codec.Encode(new JoinRequestCommand(1, uid, uid, Colors.Red)));

    private WorldDependencies Dependencies() =>
        new(TimeProvider.System, new PersistenceModel(), new SessionModel(), _codec, new ManualFrameProvider());

    private class PlayerChatHandler(List<string> calls) : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(PlayerModel sender, SendChatMessageCommand command)
        {
            calls.Add($"validate {sender.Uid}: {command.Text}");
            return command.Text switch
            {
                ThrowInValidate => throw new InvalidOperationException("validate failed"),
                Invalid => false,
                _ => true,
            };
        }

        public void Process(PlayerModel sender, SendChatMessageCommand command)
        {
            calls.Add($"process {sender.Uid}: {command.Text}");
            if (command.Text == ThrowInProcess) throw new InvalidOperationException("process failed");
        }
    }

    private class WindowChatHandler(List<string> calls) : IDedicatedWindowCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(SendChatMessageCommand command)
        {
            calls.Add($"validate window: {command.Text}");
            return command.Text switch
            {
                ThrowInValidate => throw new InvalidOperationException("validate failed"),
                Invalid => false,
                _ => true,
            };
        }

        public void Process(SendChatMessageCommand command) => calls.Add($"process window: {command.Text}");
    }

    private class PlayerJoinHandler : IPlayerCommandHandler<JoinRequestCommand>
    {
        public bool Validate(PlayerModel sender, JoinRequestCommand command) => true;

        public void Process(PlayerModel sender, JoinRequestCommand command) { }
    }

    // Stands for the join of task 015: binds the peer and adds the player, which is all the dispatcher looks at
    private class FakeJoinHandler(List<string> calls, PeerUidMap peers, PersistenceModel persistence)
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
            persistence.AddPlayer(command.Uid);
            peers.Bind(command.Uid, peerId);
        }
    }
}
