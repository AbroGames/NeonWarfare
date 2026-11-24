using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Transport;

[TestSuite]
public class HostTransportTests
{
    private const int LocalPeer = 1;
    private const int RemotePeer = 2;

    private static readonly byte[] Body = [1, 2];

    private TransportWorlds _worlds = null!;
    private RecordingLocalPlayerOwner _owner = null!;

    [BeforeTest]
    public void SetUp()
    {
        _worlds = new TransportWorlds();
        _owner = new RecordingLocalPlayerOwner();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SendJoinRequest_TheHostJoinsItsWorld_TheOwnerIsTold()
    {
        (World world, HostTransport transport) = HostWorld();

        transport.SendJoinRequest(TestWorldSetups.LocalPlayer());
        TransportWorlds.Tick(world);

        AssertThat(_owner.JoinedCount).IsEqual(1);
        AssertThat(world.Get<LocalPlayerPresentation>().Player.Uid).IsEqual(TestWorldSetups.LocalPlayerUid);
    }

    // The World would throw on a rejection: it reaches only the owner
    [TestCase]
    [RequireGodotRuntime]
    public void SendJoinRequest_Rejected_TheOwnerIsTold()
    {
        (World world, HostTransport transport) = HostWorld();

        transport.SendJoinRequest(new LocalPlayer(TestWorldSetups.LocalPlayerUid, "", Colors.White));
        TransportWorlds.Tick(world);

        AssertThat(_owner.Rejections).ContainsExactly(JoinRejectReason.InvalidNick);
        AssertThat(_owner.JoinedCount).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SendAndDisconnect_RemotePeer_GoToTheNetwork()
    {
        var network = new FakeNetwork();
        (_, HostTransport transport) = HostWorld(network);
        IClientsConnection clients = transport;

        clients.SendState(RemotePeer, Body);
        clients.SendSnapshot(RemotePeer, Body);
        clients.SendEvents(RemotePeer, Body);
        clients.Reject(RemotePeer, JoinRejectReason.UidInUse);
        clients.Disconnect(RemotePeer);

        AssertThat(network.Packets.Select(sent => sent.PeerId).Distinct()).ContainsExactly(RemotePeer);
        AssertThat(network.Packets.Select(sent => sent.Packet[0])).ContainsExactly(
            (byte) ServerPacketKind.State, (byte) ServerPacketKind.Snapshot, (byte) ServerPacketKind.Events,
            (byte) ServerPacketKind.JoinRejected);
        AssertThat(network.Disconnected).ContainsExactly(RemotePeer);
    }

    // The host's World is the server's: the tick loop never sends it the state
    [TestCase]
    [RequireGodotRuntime]
    public void SendStateAndSnapshot_LocalPeer_Throw()
    {
        (_, HostTransport transport) = HostWorld(new FakeNetwork());
        IClientsConnection clients = transport;

        AssertThrown(() => clients.SendState(LocalPeer, Body)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.SendSnapshot(LocalPeer, Body)).IsInstanceOf<InvalidOperationException>();
    }

    // Single player has no network
    [TestCase]
    [RequireGodotRuntime]
    public void SendAndDisconnect_RemotePeerWithoutNetwork_Throw()
    {
        (_, HostTransport transport) = HostWorld();
        IClientsConnection clients = transport;

        AssertThrown(() => clients.SendEvents(RemotePeer, Body)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.Reject(RemotePeer, JoinRejectReason.UidInUse))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.Disconnect(RemotePeer)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_LocalPeer_ItLeavesInTheNextTick()
    {
        (World world, HostTransport transport) = HostWorld();
        transport.SendJoinRequest(TestWorldSetups.LocalPlayer());
        TransportWorlds.Tick(world);

        ((IClientsConnection) transport).Disconnect(LocalPeer);
        AssertThat(world.Get<LocalPlayerPresentation>().Player).IsNotNull();
        TransportWorlds.Tick(world);

        AssertThrown(() => { _ = world.Get<LocalPlayerPresentation>().Player; })
            .IsInstanceOf<InvalidOperationException>();
    }

    private (World World, HostTransport Transport) HostWorld(FakeNetwork? network = null)
    {
        World world = AutoFree(new World())!;
        ServerTransport? remote = network != null ? new ServerTransport(network, world) : null;
        var transport = new HostTransport(world, _worlds.Codec, _owner, remote);
        _worlds.Init(world, TestWorldSetups.Host(localPlayerOwner: _owner), transport, transport);
        return (world, transport);
    }
}
