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
    public void PublishToAll_ReachesEveryPeerAndTheDedicatedWindow()
    {
        _outbox.AddDedicatedWindow();
        ChatServerMessageEvent common = Event("common");

        _outbox.PublishToAll(common);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(common);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(common);
        AssertThat(DedicatedWindowEvents()).ContainsExactly(common);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishTo_ReachesOnlyThatPlayersPeer()
    {
        _outbox.AddDedicatedWindow();
        ChatServerMessageEvent personal = Event("personal");

        _outbox.PublishTo(personal, _alice);

        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(personal);
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(DedicatedWindowEvents()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PublishToDedicatedWindow_ReachesOnlyTheDedicatedWindow()
    {
        _outbox.AddDedicatedWindow();
        ChatServerMessageEvent reply = Event("reply");

        _outbox.PublishToDedicatedWindow(reply);

        AssertThat(DedicatedWindowEvents()).ContainsExactly(reply);
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    // The headless dedicated server: the root never calls AddDedicatedWindow
    [TestCase]
    [RequireGodotRuntime]
    public void NoDedicatedWindow_PeersStillGetEventsAndWindowEventsAreDropped()
    {
        ChatServerMessageEvent common = Event("common");

        _outbox.PublishToAll(common);
        _outbox.PublishToDedicatedWindow(Event("reply"));

        AssertThat(_outbox.HasDedicatedWindow).IsFalse();
        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(common);
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(common);
        AssertThrown(() => _outbox.DrainDedicatedWindowEvents(new ArrayBufferWriter<byte>()))
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
        _outbox.AddDedicatedWindow();

        _outbox.PublishTo(Event("lost"), _offline);

        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(DedicatedWindowEvents()).IsEmpty();
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
    public void AddPeerAndAddDedicatedWindow_Twice_Throw()
    {
        _outbox.AddDedicatedWindow();

        AssertThrown(() => _outbox.AddPeer(AlicePeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _outbox.AddDedicatedWindow()).IsInstanceOf<InvalidOperationException>();
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

    private IReadOnlyList<object> DedicatedWindowEvents()
    {
        var output = new ArrayBufferWriter<byte>();
        _outbox.DrainDedicatedWindowEvents(output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }
}
