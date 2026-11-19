using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Commands;

[TestSuite]
public class CommandHandlerRegistryTests
{

    private const int AlicePeer = 2;

    [TestCase]
    [RequireGodotRuntime]
    public void Register_FindsEveryHandlerOfAnObject()
    {
        var chat = new ChatHandler();
        var session = new SessionHandler();

        CommandHandlerRegistry registry = Registered(chat, session);

        AssertThat(registry.SessionHandler).IsSame(session);
        AssertThat(registry.TryGetPlayerHandler(typeof(SendChatMessageCommand), out var handler)).IsTrue();
        AssertThat(handler.Name).IsEqual(nameof(ChatHandler));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetworkCommandTypes_HaveJoinOnlyWithSessionHandler()
    {
        AssertThat(Registered(new ChatHandler()).NetworkCommandTypes)
            .ContainsExactlyInAnyOrder(typeof(SendChatMessageCommand));
        AssertThat(Registered(new ChatHandler(), new SessionHandler()).NetworkCommandTypes)
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
                     [new SessionHandler(), new SessionHandler()],
                 })
        {
            var registry = new CommandHandlerRegistry();

            AssertThrown(() => registry.Register(handlers)).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_ObjectImplementingNoHandler_Throws()
    {
        var registry = new CommandHandlerRegistry();

        AssertThrown(() => registry.Register([new object()])).IsInstanceOf<InvalidOperationException>();
    }

    // CommandDispatcher routes every join to the session handler: a player handler of it would silently never run
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
            WorldLayer.Host, Dependencies(codec), new WorldRoot(AutoFree(new Node())!));
        IReadOnlySet<Type> whitelist = provider.GetRequiredService<CommandHandlerRegistry>().NetworkCommandTypes;
        var inbox = provider.GetRequiredService<CommandInbox>();
        var chat = new SendChatMessageCommand("hi");

        inbox.EnqueueFromPeer(AlicePeer, codec.Encode(new ChatServerMessageEvent(1, "an event")));
        inbox.EnqueueFromPeer(AlicePeer, codec.Encode(chat));

        AssertThat(whitelist).Contains(typeof(SendChatMessageCommand), typeof(JoinRequestCommand));
        AssertThat(whitelist.Where(type => !type.IsSubclassOf(typeof(Command)))).IsEmpty();
        AssertThat(whitelist.Contains(typeof(CommandInbox.PeerDisconnected))).IsFalse();
        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommandEntry(AlicePeer, chat));
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
        return new(TimeProvider.System, codec,
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection(),
            new RecordingClientsConnection(), new RecordingSaveFiles(),
            TestWorldDependencies.LocalPlayer(WorldLayer.Host),
            TestWorldDependencies.Admin(WorldLayer.Host), TestWorldDependencies.DedicatedServerOwner(WorldLayer.Host),
            TestWorldDependencies.LocalPlayerOwner(WorldLayer.Host));
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

    private class SessionHandler : IPeerSessionHandler
    {
        public bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason)
        {
            reason = default;
            return true;
        }

        public void Join(JoinRequestCommand command) { }

        public void Leave(string uid) { }
    }
}
