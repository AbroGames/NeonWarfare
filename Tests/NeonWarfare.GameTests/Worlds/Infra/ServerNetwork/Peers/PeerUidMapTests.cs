using GdUnit4;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.ServerNetwork.Peers;

[TestSuite]
public class PeerUidMapTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Bind_LooksUpBothWays()
    {
        var map = new PeerUidMap();

        map.Bind("alice", 2);

        AssertThat(map.TryGetPeerId("alice", out int peerId)).IsTrue();
        AssertThat(peerId).IsEqual(2);
        AssertThat(map.TryGetUid(2, out string? uid)).IsTrue();
        AssertThat(uid).IsEqual("alice");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Unbind_ForgetsBothWays()
    {
        var map = new PeerUidMap();
        map.Bind("alice", 2);

        map.Unbind(2);

        AssertThat(map.TryGetPeerId("alice", out _)).IsFalse();
        AssertThat(map.TryGetUid(2, out _)).IsFalse();
    }

    // After Unbind the uid and the peer are free again: a player displaced from an old peer rejoins on a new one
    [TestCase]
    [RequireGodotRuntime]
    public void Bind_AfterUnbind_Succeeds()
    {
        var map = new PeerUidMap();
        map.Bind("alice", 2);
        map.Unbind(2);

        map.Bind("alice", 3);

        AssertThat(map.TryGetPeerId("alice", out int peerId)).IsTrue();
        AssertThat(peerId).IsEqual(3);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Bind_UidOrPeerAlreadyBound_Throws()
    {
        var map = new PeerUidMap();
        map.Bind("alice", 2);

        AssertThrown(() => map.Bind("alice", 3)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Bind("bob", 2)).IsInstanceOf<InvalidOperationException>();
        AssertThat(map.TryGetUid(3, out _)).IsFalse();
        AssertThat(map.TryGetPeerId("bob", out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Unbind_UnknownPeer_Throws()
    {
        AssertThrown(() => new PeerUidMap().Unbind(2)).IsInstanceOf<InvalidOperationException>();
    }
}
