using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork.Commands;

[TestSuite]
public class CommandHandlerRegistryTests
{
    private const WorldLayer Host = WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler
                                    | WorldLayer.ServerNetwork | WorldLayer.Query | WorldLayer.Presentation
                                    | WorldLayer.ClientNetwork;

    private const int AlicePeer = 2;

    [TestCase]
    [RequireGodotRuntime]
    public void Register_FindsEveryHandlerOfAnObject()
    {
        var chat = new ChatHandler();
        var joinLeave = new JoinLeaveHandler();

        CommandHandlerRegistry registry = Registered(chat, joinLeave);

        AssertThat(registry.JoinHandler).IsSame(joinLeave);
        AssertThat(registry.DisconnectedHandler).IsSame(joinLeave);
        AssertThat(registry.TryGetPlayerHandler(typeof(SendChatMessageCommand), out var handler)).IsTrue();
        AssertThat(handler.Name).IsEqual(nameof(ChatHandler));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetworkCommandTypes_HaveJoinOnlyWithJoinHandler()
    {
        AssertThat(Registered(new ChatHandler()).NetworkCommandTypes)
            .ContainsExactlyInAnyOrder(typeof(SendChatMessageCommand));
        AssertThat(Registered(new ChatHandler(), new JoinLeaveHandler()).NetworkCommandTypes)
            .ContainsExactlyInAnyOrder(typeof(SendChatMessageCommand), typeof(JoinRequestCommand));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetworkCommandTypes_BeforeRegister_Throw()
    {
        var registry = new CommandHandlerRegistry();

        AssertThat(registry.IsRegistered).IsFalse();
        AssertThrown(() => _ = registry.NetworkCommandTypes).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_Twice_Throws()
    {
        CommandHandlerRegistry registry = Registered(new ChatHandler());

        AssertThrown(() => registry.Register([])).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_SecondHandlerOfOneCommand_Throws()
    {
        foreach (object[] handlers in new[]
                 {
                     new object[] { new ChatHandler(), new ChatHandler() },
                     [new JoinLeaveHandler(), new JoinLeaveHandler()],
                     [new JoinLeaveHandler(), new LeaveOnlyHandler()],
                 })
        {
            var registry = new CommandHandlerRegistry();

            AssertThrown(() => registry.Register(handlers)).IsInstanceOf<InvalidOperationException>();
        }
    }

    // A joined peer could never leave, or a leave handler would wait for joins that never come
    [TestCase]
    [RequireGodotRuntime]
    public void Register_JoinAndLeaveHandlersNotInPair_Throws()
    {
        foreach (object handler in new object[] { new JoinOnlyHandler(), new LeaveOnlyHandler() })
        {
            var registry = new CommandHandlerRegistry();

            AssertThrown(() => registry.Register([handler])).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_ObjectImplementingNoHandler_Throws()
    {
        var registry = new CommandHandlerRegistry();

        AssertThrown(() => registry.Register([new object()])).IsInstanceOf<InvalidOperationException>();
    }

    // CommandDispatcher routes every join to the join handler: a player handler of it would silently never run
    [TestCase]
    [RequireGodotRuntime]
    public void Register_PlayerHandlerOfJoinRequest_Throws()
    {
        var registry = new CommandHandlerRegistry();

        AssertThrown(() => registry.Register([new PlayerJoinHandler()])).IsInstanceOf<InvalidOperationException>();
    }

    // The real game types through the composition root: the whitelist is built from the handlers it found and
    // really reaches the inbox
    [TestCase]
    [RequireGodotRuntime]
    public void Build_Host_WhitelistHasOnlyCommandsAndReachesTheInbox()
    {
        var codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        using ServiceProvider provider = new WorldServicesBuilder().Build(
            Host, Dependencies(codec), new WorldRoot(AutoFree(new Node())!));
        IReadOnlySet<Type> whitelist = provider.GetRequiredService<CommandHandlerRegistry>().NetworkCommandTypes;
        var inbox = provider.GetRequiredService<CommandInbox>();
        var chat = new SendChatMessageCommand("hi");

        inbox.EnqueueFromPeer(AlicePeer, codec.Encode(new ChatServerMessageEvent(1, "an event")));
        inbox.EnqueueFromPeer(AlicePeer, codec.Encode(chat));

        AssertThat(whitelist).Contains(typeof(SendChatMessageCommand), typeof(JoinRequestCommand));
        AssertThat(whitelist.Where(type => !type.IsSubclassOf(typeof(Command)))).IsEmpty();
        AssertThat(whitelist.Contains(typeof(CommandInbox.PeerDisconnected))).IsFalse();
        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommand(AlicePeer, chat));
    }

    private static CommandHandlerRegistry Registered(params object[] handlers)
    {
        var registry = new CommandHandlerRegistry();
        registry.Register(handlers);
        return registry;
    }

    private static WorldDependencies Dependencies(NetMessageCodec codec)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new(TimeProvider.System, codec, new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection());
    }

    private class ChatHandler : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(string senderUid, SendChatMessageCommand command) => true;

        public void Process(string senderUid, SendChatMessageCommand command) { }
    }

    private class PlayerJoinHandler : IPlayerCommandHandler<JoinRequestCommand>
    {
        public bool Validate(string senderUid, JoinRequestCommand command) => true;

        public void Process(string senderUid, JoinRequestCommand command) { }
    }

    private class JoinLeaveHandler : IJoinRequestHandler, IPeerDisconnectedHandler
    {
        public bool Validate(JoinRequestCommand command, out JoinRejectReason reason)
        {
            reason = default;
            return true;
        }

        public void Process(JoinRequestCommand command) { }

        public void Process(string uid) { }
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
