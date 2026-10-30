using System.Buffers;
using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Hud;
using NeonWarfare.Scenes.World.Infra.Protocol;
using RepliCAT;
using static GdUnit4.Assertions;
using static NeonWarfare.Scenes.World.Features.Chat.ChatPresentation;

namespace NeonWarfare.GameTests.World.Infra.ClientNetwork;

[TestSuite]
public class EventDispatcherTests
{

    private static readonly HashSet<Type> EventTypes =
    [
        typeof(ChatPlayerMessageEvent), typeof(ChatServerMessageEvent), typeof(PlayerJoinedEvent),
        typeof(PlayerLeftEvent),
    ];

    private NetMessageCodec _codec = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
    }

    // Built through the world container: the composition root registering the handlers is covered too
    [TestCase]
    [RequireGodotRuntime]
    public void Dispatch_ChatEvents_ReachChatPresentationInOrder()
    {
        using ServiceProvider provider = new WorldServicesBuilder().Build(
            WorldLayer.Client, Dependencies(), new WorldRoot(AutoFree(new Node())!));
        byte[] section = Section(
            new PlayerJoinedEvent(1, "alice", "Alice"),
            new ChatPlayerMessageEvent(2, "alice", "Alice", "hi"),
            new ChatServerMessageEvent(3, "server"),
            new PlayerLeftEvent(4, "alice", "Alice"));

        int bytesRead = provider.GetRequiredService<EventDispatcher>().Dispatch(section);

        AssertThat(bytesRead).IsEqual(section.Length);
        AssertThat(provider.GetRequiredService<ChatPresentation>().Entries).ContainsExactly(
            new PlayerJoinedEntry(1, "alice", "Alice"),
            new PlayerMessageEntry(2, "alice", "Alice", "hi"),
            new ServerTextEntry(3, "server"),
            new PlayerLeftEntry(4, "alice", "Alice"));
        AssertThat(provider.GetRequiredService<HudMailbox>().Read<ChatEntryAddedNotice>()).HasSize(4);
    }

    // The throwing handler is registered first, so it runs before ChatPresentation on the very same event
    [TestCase]
    [RequireGodotRuntime]
    public void Dispatch_ThrowingHandler_DoesNotStopTheBatch()
    {
        var chat = new ChatPresentation(new HudMailbox(new ManualFrameProvider()));
        EventDispatcher dispatcher = Dispatcher(new ThrowingPresentation(), chat);

        dispatcher.Dispatch(Section(new ChatServerMessageEvent(1, "first"), new ChatServerMessageEvent(2, "second")));

        AssertThat(chat.Entries).ContainsExactly(new ServerTextEntry(1, "first"), new ServerTextEntry(2, "second"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Dispatch_BrokenSection_ThrowsAndCallsNothing()
    {
        var chat = new ChatPresentation(new HudMailbox(new ManualFrameProvider()));
        EventDispatcher dispatcher = Dispatcher(chat);
        byte[] section = Section(new ChatServerMessageEvent(1, "first"), new ChatServerMessageEvent(2, "second"));

        NetMessageCodecTests.AssertRejected(() => dispatcher.Dispatch(section.AsMemory(..^1)));
        AssertThat(chat.Entries).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DispatchPacket_EventsPacket_ReachesHandlers()
    {
        var chat = new ChatPresentation(new HudMailbox(new ManualFrameProvider()));
        EventDispatcher dispatcher = Dispatcher(chat);

        dispatcher.DispatchPacket(Packet(ServerPacketKind.Events, Section(new ChatServerMessageEvent(1, "first"))));

        AssertThat(chat.Entries).ContainsExactly(new ServerTextEntry(1, "first"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DispatchPacket_EmptyWrongKindOrTrailingBytes_ThrowsAndCallsNothing()
    {
        var chat = new ChatPresentation(new HudMailbox(new ManualFrameProvider()));
        EventDispatcher dispatcher = Dispatcher(chat);
        byte[] section = Section(new ChatServerMessageEvent(1, "first"));

        foreach (byte[] packet in new[]
                 {
                     [],
                     Packet((ServerPacketKind) 0, section),
                     Packet(ServerPacketKind.Events, [..section, 0]),
                 })
        {
            NetMessageCodecTests.AssertRejected(() => dispatcher.DispatchPacket(packet));
        }
        AssertThat(chat.Entries).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Dispatch_PrivateHandlerOfBaseClass_IsCalled()
    {
        var derived = new DerivedPresentation();
        EventDispatcher dispatcher = Dispatcher(derived);

        dispatcher.Dispatch(Section(new ChatServerMessageEvent(1, "first")));

        AssertThat(derived.Received).ContainsExactly("first");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_HandlerOfNonEventType_Throws()
    {
        AssertThrown(() => Dispatcher(new NonEventPresentation()))
            .IsInstanceOf<InvalidOperationException>();
    }

    private EventDispatcher Dispatcher(params object[] presentations)
    {
        var dispatcher = new EventDispatcher(_codec);
        dispatcher.Register(presentations, EventTypes);
        return dispatcher;
    }

    private byte[] Section(params object[] events)
    {
        var buffer = new ArrayBufferWriter<byte>();
        _codec.WriteSection(buffer, events.Select(@event => (ReadOnlyMemory<byte>) _codec.Encode(@event)).ToList());
        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] Packet(ServerPacketKind kind, byte[] section) => [(byte) kind, ..section];

    // A frame that never ends: everything posted during the test is still readable at its end
    private WorldDependencies Dependencies()
    {
        WorldPackedScenes scenes = AutoFree(new WorldPackedScenes())!;
        return new(TimeProvider.System, _codec,
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), new RecordingClientsConnection(),
            new RecordingClientsConnection());
    }

    private class ThrowingPresentation
    {
        [EventHandler]
        private void Handle(ChatServerMessageEvent e) => throw new InvalidOperationException("handler failed");
    }

    private class BasePresentation
    {
        public List<string> Received { get; } = [];

        [EventHandler]
        private void Handle(ChatServerMessageEvent e) => Received.Add(e.Text);
    }

    private class DerivedPresentation : BasePresentation;

    private class NonEventPresentation
    {
        [EventHandler]
        private void Handle(string text) { }
    }
}
