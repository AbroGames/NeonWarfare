using GdUnit4;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Game.Networks.Fixtures;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Worlds;
using static GdUnit4.Assertions;
using Call = NeonWarfare.GameTests.Game.Fixtures.RecordingWorldEndpoint.Call;

namespace NeonWarfare.GameTests.Game.Networks.Transport;

[TestSuite]
public class ServerTransportTests
{
    private const int RemotePeer = 2;

    private FakeNetwork _network = null!;
    private RecordingWorldEndpoint _endpoint = null!;
    private ServerTransport _transport = null!;

    [BeforeTest]
    public void SetUp()
    {
        _network = new FakeNetwork();
        _endpoint = new RecordingWorldEndpoint();
        _transport = new ServerTransport(_network, _endpoint);
    }

    [TestCase]
    public void PeerConnected_StartsItsHandshake()
    {
        _network.Connect(RemotePeer);

        AssertThat(_endpoint.Calls).ContainsExactly(new Call(nameof(World.IServerEndpoint.StartHandshake), RemotePeer));
    }

    [TestCase]
    public void PeerDisconnected_QueuesItsDisconnection()
    {
        _network.Drop(RemotePeer);

        AssertThat(_endpoint.Calls)
            .ContainsExactly(new Call(nameof(World.IServerEndpoint.QueueDisconnection), RemotePeer));
    }

    [TestCase]
    public void PacketReceived_GoesToTheWorldAsIs()
    {
        _network.Receive(RemotePeer, [1, 2]);

        Call call = _endpoint.Calls.Single();
        AssertThat(call.Name).IsEqual(nameof(World.IServerEndpoint.ReceiveFromClient));
        AssertThat(call.PeerId).IsEqual(RemotePeer);
        AssertThat(call.Body).ContainsExactly(1, 2);
    }

    // A World that failed to be built is freed, while the network lives on
    [TestCase]
    public void Dispose_DetachesFromTheNetwork()
    {
        _transport.Dispose();

        _network.Connect(RemotePeer);
        _network.Drop(RemotePeer);
        _network.Receive(RemotePeer, [1]);

        AssertThat(_endpoint.Calls).IsEmpty();
        AssertThat(_network.HasListeners).IsFalse();
    }
}
