using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.ServerNetwork;

// Every check reads the events back through the codec: what matters is the section a peer receives
[TestSuite]
public class EventOutboxTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private static readonly HashSet<Type> EventTypes =
        [typeof(ChatServerMessageEvent), typeof(ChatPlayerMessageEvent), typeof(PlayerJoinedEvent)];

    private readonly PlayerModel _alice = new("alice");
    private readonly PlayerModel _bob = new("bob");
    private readonly PlayerModel _offline = new("offline");

    private NetMessageCodec _codec = null!;
    private PeerUidMap _peers = null!;
    private EventOutbox _outbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        _peers = new PeerUidMap();
        _outbox = new EventOutbox(_codec, _peers);
        Join(_alice, AlicePeer);
        Join(_bob, BobPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishToAll_ReachesEveryPeerAndTheConsole()
    {
        _outbox.AddConsole();
        ChatServerMessageEvent common = Event("common");

        _outbox.PublishToAll(common);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(common);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(common);
        AssertThat(ConsoleEvents()).ContainsExactly(common);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_ReachesOnlyThatPlayersPeer()
    {
        _outbox.AddConsole();
        ChatServerMessageEvent personal = Event("personal");

        _outbox.PublishTo(personal, _alice);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(personal);
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(ConsoleEvents()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishToConsole_ReachesOnlyTheConsole()
    {
        _outbox.AddConsole();
        ChatServerMessageEvent reply = Event("reply");

        _outbox.PublishToConsole(reply);

        AssertThat(ConsoleEvents()).ContainsExactly(reply);
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    // The headless dedicated server: the root never calls AddConsole
    [TestCase]
    [RequireGodotRuntime]
    public void NoConsole_PeersStillGetEventsAndConsoleEventsAreDropped()
    {
        ChatServerMessageEvent common = Event("common");

        _outbox.PublishToAll(common);
        _outbox.PublishToConsole(Event("reply"));

        AssertThat(_outbox.HasConsole).IsFalse();
        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(common);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(common);
        AssertThrown(() => _outbox.DrainConsoleEvents(new ArrayBufferWriter<byte>()))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PersonalAndCommonEvents_KeepTheOrderOfPublication()
    {
        ChatServerMessageEvent first = Event("first");
        ChatServerMessageEvent second = Event("second");
        var third = new PlayerJoinedEvent(3, "carol", "Carol");
        ChatServerMessageEvent fourth = Event("fourth");

        _outbox.PublishToAll(first);
        _outbox.PublishTo(second, _alice);
        _outbox.PublishToAll(third);
        _outbox.PublishTo(fourth, _alice);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(first, second, third, fourth);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(first, third);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_OfflinePlayer_TouchesNoBuffer()
    {
        _outbox.AddConsole();

        _outbox.PublishTo(Event("lost"), _offline);

        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(ConsoleEvents()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_BoundPeerWithoutBuffer_Throws()
    {
        _peers.Bind(_offline.Uid, 4);

        AssertThrown(() => _outbox.PublishTo(Event("lost"), _offline)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DrainPeerEvents_EmptiesTheBufferAndCountsTheEvents()
    {
        _outbox.PublishToAll(Event("one"));
        _outbox.PublishToAll(Event("two"));

        AssertThat(_outbox.DrainPeerEvents(AlicePeer, new ArrayBufferWriter<byte>())).IsEqual(2);
        AssertThat(_outbox.DrainPeerEvents(AlicePeer, new ArrayBufferWriter<byte>())).IsEqual(0);
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AddPeer_Later_GetsNoEarlierEvents()
    {
        _outbox.PublishToAll(Event("before"));
        Join(new PlayerModel("carol"), 4);
        ChatServerMessageEvent after = Event("after");

        _outbox.PublishToAll(after);

        AssertThat(PeerEvents(4)).ContainsExactly(after);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RemovePeer_LeavesTheOutbox()
    {
        _outbox.RemovePeer(BobPeer);

        _outbox.PublishToAll(Event("common"));

        AssertThat(_outbox.Peers).ContainsExactly(AlicePeer);
        AssertThrown(() => _outbox.DrainPeerEvents(BobPeer, new ArrayBufferWriter<byte>()))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _outbox.RemovePeer(BobPeer)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AddPeerAndAddConsole_Twice_Throw()
    {
        _outbox.AddConsole();

        AssertThrown(() => _outbox.AddPeer(AlicePeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _outbox.AddConsole()).IsInstanceOf<InvalidOperationException>();
    }

    private void Join(PlayerModel player, int peerId)
    {
        _peers.Bind(player.Uid, peerId);
        _outbox.AddPeer(peerId);
    }

    private static ChatServerMessageEvent Event(string text) => new(1, text);

    private IReadOnlyList<object> PeerEvents(int peerId)
    {
        var output = new ArrayBufferWriter<byte>();
        _outbox.DrainPeerEvents(peerId, output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }

    private IReadOnlyList<object> ConsoleEvents()
    {
        var output = new ArrayBufferWriter<byte>();
        _outbox.DrainConsoleEvents(output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }
}
