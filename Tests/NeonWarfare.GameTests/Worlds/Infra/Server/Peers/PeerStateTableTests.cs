using GdUnit4;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Peers;

[TestSuite]
public class PeerStateTableTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;
    private const string Alice = "alice";

    private static readonly DateTimeOffset Deadline = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    private PeerStateTable _peers = null!;

    [BeforeTest]
    public void SetUp()
    {
        _peers = new PeerStateTable();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Connect_PeerIsNeitherJoinedNorCut()
    {
        _peers.Connect(AlicePeer, Deadline);

        AssertThat(_peers.IsJoined(AlicePeer)).IsFalse();
        AssertThat(_peers.IsCut(AlicePeer)).IsFalse();
        AssertThrown(() => _peers.Connect(AlicePeer, Deadline)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ExpiredConnecting_OnlyConnectingPeersPastTheirDeadline()
    {
        _peers.Connect(AlicePeer, Deadline);
        _peers.Connect(BobPeer, Deadline + TimeSpan.FromSeconds(1));
        ConnectAndJoin(4, Alice);

        AssertThat(_peers.ExpiredConnecting(Deadline - TimeSpan.FromMilliseconds(1))).IsEmpty();
        AssertThat(_peers.ExpiredConnecting(Deadline)).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ConnectingPeer_LooksUpBothWays()
    {
        _peers.Connect(AlicePeer, Deadline);
        _peers.Connect(BobPeer, Deadline);

        _peers.Join(AlicePeer, Alice);
        _peers.Join(BobPeer, "bob");

        AssertThat(_peers.TryGetJoined(AlicePeer, out PeerStateTable.Joined? joined)).IsTrue();
        AssertThat(joined!.Uid).IsEqual(Alice);
        AssertThat(_peers.TryGetJoinedByUid(Alice, out PeerStateTable.Joined? byUid)).IsTrue();
        AssertThat(byUid).IsSame(joined);
        AssertThat(_peers.TryGetPeerIdByUid(Alice, out int peerId)).IsTrue();
        AssertThat(peerId).IsEqual(AlicePeer);
        AssertThat(_peers.JoinedPeerIds).ContainsExactlyInAnyOrder(AlicePeer, BobPeer);
        AssertThat(_peers.AllJoined.Count()).IsEqual(2);
        AssertThat(_peers.ExpiredConnecting(Deadline)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_UidTakenOrPeerNotConnecting_Throws()
    {
        ConnectAndJoin(AlicePeer, Alice);
        _peers.Connect(BobPeer, Deadline);

        AssertThrown(() => _peers.Join(BobPeer, Alice)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _peers.Join(AlicePeer, "other")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _peers.Join(4, "other")).IsInstanceOf<InvalidOperationException>();
        _peers.Disconnect(BobPeer);
        AssertThrown(() => _peers.Join(BobPeer, "bob")).IsInstanceOf<InvalidOperationException>();

        AssertThat(_peers.TryGetPeerIdByUid("other", out _)).IsFalse();
        AssertThat(_peers.TryGetPeerIdByUid("bob", out _)).IsFalse();
        AssertThat(_peers.JoinedPeerIds).ContainsExactly(AlicePeer);
    }

    // The transport is told once per peer, however many paths cut it off
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_ReportsTheChangeOnce()
    {
        _peers.Connect(AlicePeer, Deadline);
        ConnectAndJoin(BobPeer, "bob");

        AssertThat(_peers.Disconnect(AlicePeer)).IsTrue();
        AssertThat(_peers.Disconnect(AlicePeer)).IsFalse();
        AssertThat(_peers.Disconnect(BobPeer)).IsTrue();
        AssertThat(_peers.Disconnect(BobPeer)).IsFalse();
        AssertThat(_peers.Disconnect(4)).IsTrue();

        AssertThat(_peers.IsCut(AlicePeer)).IsTrue();
        AssertThat(_peers.IsCut(BobPeer)).IsTrue();
        AssertThat(_peers.IsCut(4)).IsTrue();
        AssertThat(_peers.ExpiredConnecting(Deadline)).IsEmpty();
    }

    // Its Leave is still due, so a rejoin with its uid would otherwise lose its "online" to that Leave
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_JoinedPeer_KeepsTheUidButNoLongerReceives()
    {
        ConnectAndJoin(AlicePeer, Alice);
        _peers.Connect(BobPeer, Deadline);

        _peers.Disconnect(AlicePeer);

        AssertThat(_peers.IsJoined(AlicePeer)).IsFalse();
        AssertThat(_peers.JoinedPeerIds).IsEmpty();
        AssertThat(_peers.TryGetJoinedByUid(Alice, out _)).IsFalse();
        AssertThat(_peers.TryGetPeerIdByUid(Alice, out int peerId)).IsTrue();
        AssertThat(peerId).IsEqual(AlicePeer);
        AssertThrown(() => _peers.Join(BobPeer, Alice)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Release_LeavingPeer_FreesTheUidAndStaysCut()
    {
        ConnectAndJoin(AlicePeer, Alice);
        _peers.Disconnect(AlicePeer);

        _peers.Release(AlicePeer);
        ConnectAndJoin(BobPeer, Alice);

        AssertThat(_peers.IsCut(AlicePeer)).IsTrue();
        AssertThat(_peers.TryGetPeerIdByUid(Alice, out int peerId)).IsTrue();
        AssertThat(peerId).IsEqual(BobPeer);
        AssertThat(_peers.Remove(AlicePeer)).IsInstanceOf<PeerStateTable.TurnedAway>();
        AssertThat(_peers.TryGetPeerIdByUid(Alice, out _)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Release_PeerNotLeaving_Throws()
    {
        _peers.Connect(AlicePeer, Deadline);
        ConnectAndJoin(BobPeer, "bob");
        _peers.Disconnect(4);

        AssertThrown(() => _peers.Release(AlicePeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _peers.Release(BobPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _peers.Release(4)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _peers.Release(5)).IsInstanceOf<InvalidOperationException>();
        AssertThat(_peers.IsJoined(BobPeer)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Remove_ReturnsTheLastStateAndFreesTheUid()
    {
        ConnectAndJoin(AlicePeer, Alice);
        ConnectAndJoin(BobPeer, "bob");
        _peers.Disconnect(BobPeer);

        AssertThat(((PeerStateTable.Joined) _peers.Remove(AlicePeer)).Uid).IsEqual(Alice);
        AssertThat(_peers.Remove(BobPeer)).IsEqual(new PeerStateTable.Leaving("bob"));
        AssertThat(_peers.Remove(AlicePeer)).IsNull();

        AssertThat(_peers.TryGetPeerIdByUid(Alice, out _)).IsFalse();
        AssertThat(_peers.TryGetPeerIdByUid("bob", out _)).IsFalse();
        AssertThat(_peers.IsCut(BobPeer)).IsFalse();
        // ENet may give a later peer the same id
        ConnectAndJoin(AlicePeer, Alice);
    }

    private void ConnectAndJoin(int peerId, string uid)
    {
        _peers.Connect(peerId, Deadline);
        _peers.Join(peerId, uid);
    }
}
