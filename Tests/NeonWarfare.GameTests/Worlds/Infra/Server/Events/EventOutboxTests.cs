using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Events;

// Every check reads the events back through the codec: what matters is the section a peer receives
[TestSuite]
public class EventOutboxTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private static readonly HashSet<Type> EventTypes =
        [typeof(ChatServerMessageEvent), typeof(ChatPlayerMessageEvent), typeof(LocalizedChatMessageEvent)];

    private const string Alice = "alice";
    private const string Bob = "bob";
    private const string Offline = "offline";

    private NetMessageCodec _codec = null!;
    private PeerStateTable _peers = null!;
    private EventOutbox _outbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _peers = new PeerStateTable();
        _outbox = new EventOutbox(_codec, _peers);
        Join(Alice, AlicePeer);
        Join(Bob, BobPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishToAll_ReachesEveryPeer()
    {
        ChatServerMessageEvent common = Event("common");

        _outbox.PublishToAll(common);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(common);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(common);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_ReachesOnlyThatPlayersPeer()
    {
        ChatServerMessageEvent personal = Event("personal");

        _outbox.PublishTo(personal, Alice);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(personal);
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PersonalAndCommonEvents_KeepTheOrderOfPublication()
    {
        ChatServerMessageEvent first = Event("first");
        ChatServerMessageEvent second = Event("second");
        var third = new LocalizedChatMessageEvent(3, "key", ["Carol"]);
        ChatServerMessageEvent fourth = Event("fourth");

        _outbox.PublishToAll(first);
        _outbox.PublishTo(second, Alice);
        _outbox.PublishToAll(third);
        _outbox.PublishTo(fourth, Alice);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(first, second, third, fourth);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(first, third);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_OfflinePlayer_TouchesNoBuffer()
    {
        _outbox.PublishTo(Event("lost"), Offline);

        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    // Disconnected by the server, the peer keeps its uid until its peer_disconnected, but receives nothing more
    [TestCase]
    [RequireGodotRuntime]
    public void Publish_LeavingPeer_IsSkipped()
    {
        _peers.Disconnect(BobPeer);

        _outbox.PublishTo(Event("personal"), Bob);
        _outbox.PublishToAll(Event("common"));

        AssertThat(_peers.TryGetPeerIdByUid(Bob, out _)).IsTrue();
        AssertThrown(() => _outbox.DrainEvents(BobPeer, new ArrayBufferWriter<byte>()))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DrainEvents_EmptiesTheBufferAndCountsTheEvents()
    {
        _outbox.PublishToAll(Event("one"));
        _outbox.PublishToAll(Event("two"));

        AssertThat(_outbox.DrainEvents(AlicePeer, new ArrayBufferWriter<byte>())).IsEqual(2);
        AssertThat(_outbox.DrainEvents(AlicePeer, new ArrayBufferWriter<byte>())).IsEqual(0);
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_Later_GetsNoEarlierEvents()
    {
        _outbox.PublishToAll(Event("before"));
        Join("carol", 4);
        ChatServerMessageEvent after = Event("after");

        _outbox.PublishToAll(after);

        AssertThat(PeerEvents(4)).ContainsExactly(after);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RemovedPeer_LeavesTheOutbox()
    {
        _peers.Remove(BobPeer);

        _outbox.PublishToAll(Event("common"));
        _outbox.PublishTo(Event("personal"), Bob);

        AssertThat(PeerEvents(AlicePeer).Count).IsEqual(1);
        AssertThrown(() => _outbox.DrainEvents(BobPeer, new ArrayBufferWriter<byte>()))
            .IsInstanceOf<InvalidOperationException>();
    }

    private void Join(string uid, int peerId)
    {
        _peers.Connect(peerId, DateTimeOffset.MaxValue);
        _peers.Join(peerId, uid);
    }

    private static ChatServerMessageEvent Event(string text) => new(1, text);

    private IReadOnlyList<object> PeerEvents(int peerId)
    {
        var output = new ArrayBufferWriter<byte>();
        _outbox.DrainEvents(peerId, output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }
}
