using GdUnit4;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Transport;

[TestSuite]
public class ServerTransportTests
{
    private const int RemotePeer = 2;

    private TransportWorlds _worlds = null!;
    private FakeNetwork _network = null!;

    [BeforeTest]
    public void SetUp()
    {
        _worlds = new TransportWorlds();
        _network = new FakeNetwork();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PeerConnected_NotJoinedByTheDeadline_IsDisconnected()
    {
        World world = DedicatedWorld();

        _network.Connect(RemotePeer);
        _worlds.Time.Now += PeerGatekeeper.HandshakeTimeout;
        TransportWorlds.Tick(world);

        AssertThat(_network.Disconnected).ContainsExactly(RemotePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PacketReceived_TheJoinOfAConnectedPeer_JoinsIt()
    {
        World world = DedicatedWorld();

        Join(world);

        AssertThat(world.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Uid))
            .ContainsExactly(TestWorldSetups.LocalPlayerUid);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PeerDisconnected_TheJoinedPeerLeavesInTheNextTick()
    {
        World world = DedicatedWorld();
        Join(world);

        _network.Drop(RemotePeer);
        AssertThat(world.Get<PlayerQuery>().OnlinePlayers().Count()).IsEqual(1);
        TransportWorlds.Tick(world);

        AssertThat(world.Get<PlayerQuery>().OnlinePlayers()).IsEmpty();
    }

    // A World that failed to be built is freed, while the network lives on
    [TestCase]
    [RequireGodotRuntime]
    public void Dispose_DetachesFromTheNetwork()
    {
        World world = AutoFree(new World())!;
        var transport = new ServerTransport(_network, world);

        transport.Dispose();

        // The World is not initialized, so each of these would throw if it still reached it
        _network.Connect(RemotePeer);
        _network.Drop(RemotePeer);
        _network.Receive(RemotePeer, [1]);
    }

    private World DedicatedWorld()
    {
        World world = AutoFree(new World())!;
        var transport = new ServerTransport(_network, world);
        return _worlds.Init(world, TestWorldSetups.Dedicated(), transport, new NoConnection());
    }

    private void Join(World world)
    {
        _network.Connect(RemotePeer);
        _network.Receive(RemotePeer,
            _worlds.Codec.Encode(TestWorldSetups.LocalPlayer().ToJoinRequest(_worlds.Codec.ProtocolHash)));
        TransportWorlds.Tick(world);
    }
}
