using GdUnit4;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Game.Networks.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using static GdUnit4.Assertions;
using Call = NeonWarfare.GameTests.Game.Fixtures.RecordingWorldEndpoint.Call;

namespace NeonWarfare.GameTests.Game.Networks.Transport;

[TestSuite]
public class HostTransportTests
{
    private const int LocalPeer = 1;
    private const int RemotePeer = 2;

    private static readonly byte[] Body = [1, 2];

    private TransportWorlds _worlds = null!;
    private RecordingLocalPlayerOwner _owner = null!;
    private RecordingWorldEndpoint _endpoint = null!;

    [BeforeTest]
    public void SetUp()
    {
        _worlds = new TransportWorlds();
        _owner = new RecordingLocalPlayerOwner();
        _endpoint = new RecordingWorldEndpoint();
    }

    // The whole loopback over a real World: the join in, the tick, the events of the join back into the World
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

    // The host's own peer passes the same gatekeeper as a remote one: connected first, then its join
    [TestCase]
    [RequireGodotRuntime]
    public void SendJoinRequest_ConnectsTheLocalPeer_ThenSendsItsJoin()
    {
        HostTransport transport = FakeHost();
        LocalPlayer player = TestWorldSetups.LocalPlayer();

        transport.SendJoinRequest(player);

        AssertThat(_endpoint.Calls.Select(call => call with { Body = null })).ContainsExactly(
            new Call(nameof(World.IServerEndpoint.StartHandshake), LocalPeer),
            new Call(nameof(World.IServerEndpoint.ReceiveFromClient), LocalPeer));
        AssertThat(_endpoint.Calls[1].Body).ContainsExactly(
            _worlds.Codec.Encode(player.ToJoinRequest(TestProtocol.Hash)));
    }

    // The World would throw on a disconnect reason: it reaches only the owner
    [TestCase]
    [RequireGodotRuntime]
    public void SendDisconnectReason_LocalPeer_ReachesOnlyTheOwner()
    {
        IClientsConnection clients = FakeHost();

        clients.SendDisconnectReason(LocalPeer, DisconnectReason.InvalidNick);

        AssertThat(_owner.Disconnections).ContainsExactly(DisconnectReason.InvalidNick);
        AssertThat(_endpoint.Calls).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SendTickUpdate_LocalPeer_LoopsTheEventsBackIntoTheWorld()
    {
        IClientsConnection clients = FakeHost();

        clients.SendTickUpdate(LocalPeer, 7, [], Body);

        Call call = _endpoint.Calls.Single();
        AssertThat(call.Name).IsEqual(nameof(World.IHostEndpoint.ReceiveOwnEvents));
        AssertThat(call.Events).ContainsExactly(Body);
    }

    // Delivered inside the server's tick: a throw would cost the remote peers their events, false would cut the host
    [TestCase]
    [RequireGodotRuntime]
    public void SendTickUpdate_LocalPeerWorldThrows_ReportsSent()
    {
        IClientsConnection clients = FakeHost();
        _endpoint.ThrowOnReceive = true;

        AssertThat(clients.SendTickUpdate(LocalPeer, 1, [], Body)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SendAndDisconnect_RemotePeer_GoToTheNetwork()
    {
        var network = new FakeNetwork();
        IClientsConnection clients = FakeHost(network);

        clients.SendTickUpdate(RemotePeer, 1, Body, Body);
        clients.SendSnapshot(RemotePeer, 1, Body);
        clients.SendDisconnectReason(RemotePeer, DisconnectReason.UidInUse);
        clients.Disconnect(RemotePeer);

        AssertThat(network.Packets.Select(sent => sent.PeerId).Distinct()).ContainsExactly(RemotePeer);
        AssertThat(network.Packets.Select(sent => sent.Packet[0])).ContainsExactly(
            (byte) ServerPacketKind.TickUpdate, (byte) ServerPacketKind.Snapshot,
            (byte) ServerPacketKind.Disconnecting);
        AssertThat(network.Disconnected).ContainsExactly(RemotePeer);
        AssertThat(_endpoint.Calls).IsEmpty();
    }

    // The host's World is the server's: the tick loop never sends it the state
    [TestCase]
    [RequireGodotRuntime]
    public void SendStateAndSnapshot_LocalPeer_Throw()
    {
        IClientsConnection clients = FakeHost(new FakeNetwork());

        AssertThrown(() => clients.SendTickUpdate(LocalPeer, 1, Body, Body))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.SendSnapshot(LocalPeer, 1, Body)).IsInstanceOf<InvalidOperationException>();
        AssertThat(_endpoint.Calls).IsEmpty();
    }

    // Single player has no network
    [TestCase]
    [RequireGodotRuntime]
    public void SendAndDisconnect_RemotePeerWithoutNetwork_Throw()
    {
        IClientsConnection clients = FakeHost();

        AssertThrown(() => clients.SendTickUpdate(RemotePeer, 1, [], Body))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.SendDisconnectReason(RemotePeer, DisconnectReason.UidInUse))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => clients.Disconnect(RemotePeer)).IsInstanceOf<InvalidOperationException>();
    }

    // Queued like any other disconnection: the World processes it in the next tick
    [TestCase]
    [RequireGodotRuntime]
    public void Disconnect_LocalPeer_QueuesItsDisconnection()
    {
        IClientsConnection clients = FakeHost();

        clients.Disconnect(LocalPeer);

        AssertThat(_endpoint.Calls)
            .ContainsExactly(new Call(nameof(World.IServerEndpoint.QueueDisconnection), LocalPeer));
    }

    private HostTransport FakeHost(FakeNetwork? network = null)
    {
        ServerTransport? remote = network != null ? new ServerTransport(network, _endpoint) : null;
        return new HostTransport(_endpoint, _endpoint, _worlds.Codec, TestProtocol.Hash, _owner, remote);
    }

    private (World World, HostTransport Transport) HostWorld()
    {
        World world = AutoFree(new World())!;
        var transport = new HostTransport(world, world, _worlds.Codec, TestProtocol.Hash, _owner, null);
        _worlds.Init(world, TestWorldSetups.Host(localPlayerOwner: _owner), transport, transport);
        return (world, transport);
    }
}
