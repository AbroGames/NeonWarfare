using System.Buffers;
using GdUnit4;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.ClientNetwork;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Notices;
using NeonWarfare.Scenes.World.Presentations;
using NeonWarfare.Scenes.World.Protocol;
using static GdUnit4.Assertions;
using static NeonWarfare.Scenes.World.Presentations.ChatPresentation;

namespace NeonWarfare.GameTests.World.ClientNetwork;

[TestSuite]
public class EventDispatcherTests
{
    private const WorldLayer Client = WorldLayer.Query | WorldLayer.Presentation | WorldLayer.ClientNetwork;

    private static readonly HashSet<Type> EventTypes =
    [
        typeof(ChatPlayerMessageEvent), typeof(ChatServerMessageEvent), typeof(PlayerJoinedEvent),
        typeof(PlayerLeftEvent),
    ];

    private NetMessageCodec _codec = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
    }

    // Built through the world container: the composition root registering the handlers is covered too
    [TestCase]
    [RequireGodotRuntime]
    public void Dispatch_ChatEvents_ReachChatPresentationInOrder()
    {
        using ServiceProvider provider = new WorldServicesBuilder().Build(Client, Dependencies());
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

    // A frame that never ends: everything posted during the test is still readable at its end
    private WorldDependencies Dependencies() =>
        new(TimeProvider.System, new PersistenceModel(), new SessionModel(), _codec, new ManualFrameProvider());

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
