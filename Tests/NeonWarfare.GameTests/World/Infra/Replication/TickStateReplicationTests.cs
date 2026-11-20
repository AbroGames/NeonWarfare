using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.ClientReplication;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using RepliCAT;
using RepliCAT.Bits;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World.Infra.Replication;

// A host and Alice's remote client, both through the real composition root, joined by the fake transport the way
// Game joins them. Until 020 spawns entities on clients, Alice's storages are created here and registered under the
// server's NetIds; until 021 she has no snapshot, so she joins before the first tick, whose deltas are complete
[TestSuite]
public class TickStateReplicationTests
{
    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;
    private const int BobPeer = 3;
    private const string HostUid = "HostHostHo-Hhhhhhhhhh";
    private const string AliceUid = "AliceAlice-Aaaaaaaaaa";
    private const string BobUid = "BobBobBobB-Bbbbbbbbbb";

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private Node _serverRoot = null!;
    private RecordingClientsConnection _connection = null!;
    private ServiceProvider _server = null!;
    private ServiceProvider _alice = null!;
    private readonly List<Node> _clientNodes = [];

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _serverRoot = new Node();
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _server = new WorldServicesBuilder()
            .Build(WorldLayer.Host, Dependencies(_connection), new WorldRoot(_serverRoot));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();
        _connection.Loopback = _server.GetRequiredService<EventDispatcher>().DispatchPacket;

        _alice = ClientOf(AlicePeer);
        JoinDirectly(HostUid, "Host", HostPeer);
        JoinDirectly(AliceUid, "Alice", AlicePeer);
    }

    [AfterTest]
    public void TearDown()
    {
        _alice.Dispose();
        _server.Dispose();
        _clientNodes.ForEach(node => node.Free());
        _clientNodes.Clear();
        _serverRoot.Free();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ModelChange_ReachesTheClientAfterTheTick()
    {
        Tick();
        AssertThat(AliceOnline()).ContainsExactlyInAnyOrder(HostUid, AliceUid);

        Players().PlayerByUid[AliceUid].Nick = "Renamed";
        AssertThat(_alice.GetRequiredService<PlayersStorageQuery>().Model.PlayerByUid[AliceUid].Nick)
            .IsEqual("Alice");
        Tick();

        AssertThat(_alice.GetRequiredService<PlayersStorageQuery>().Model.PlayerByUid[AliceUid].Nick)
            .IsEqual("Renamed");
    }

    // Done when: the state packet of a tick is applied before the events of the same tick are handled
    [TestCase]
    [RequireGodotRuntime]
    public void JoinEvent_IsHandledOnAClientWhoseModelsAreAlreadyAtTheEndOfTheTick()
    {
        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(_alice.GetRequiredService<JoinRecorder>().Seen)
            .ContainsExactly(new JoinRecorder.Sight(BobUid, true, "Bob"));
    }

    // Done when: a peer joined in this tick gets no state packet of this tick
    [TestCase]
    [RequireGodotRuntime]
    public void PeerJoinedInTheTick_GetsOnlyTheEventsOfIt_TheStateFromTheNextTick()
    {
        Tick();
        _connection.Packets.Clear();

        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.Events);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
        _connection.Packets.Clear();

        Players().PlayerByUid[BobUid].Nick = "Robert";
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.State);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void StatePacket_IsTheSameBytesForEveryPeer_NoneForTheHost()
    {
        JoinDirectly(BobUid, "Bob", BobPeer);

        Tick();

        AssertThat(Kinds(HostPeer)).IsEmpty();
        AssertThat(StatePacket(BobPeer)).IsEqual(StatePacket(AlicePeer));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickWithoutChanges_SendsNoStatePacket()
    {
        Tick();
        _connection.Packets.Clear();

        Tick();

        AssertThat(_connection.Packets).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void StatePacket_HasOnlyTheChangedEntities()
    {
        Tick();
        AssertThat(NetIdsIn(StatePacket(AlicePeer)))
            .ContainsExactly(ServerId<PlayersStorage>(), ServerId<PlayersSessionStorage>());
        _connection.Packets.Clear();

        Online().Remove(HostUid);
        Tick();

        AssertThat(NetIdsIn(StatePacket(AlicePeer))).ContainsExactly(ServerId<PlayersSessionStorage>());
        AssertThat(AliceOnline()).ContainsExactly(AliceUid);
    }

    // Changed, then despawned in the same tick: a delta for it would reach a client that is about to lose it
    [TestCase]
    [RequireGodotRuntime]
    public void EntityDespawnedInTheTick_HasNoDeltaInIt()
    {
        _connection.Receivers.Clear();
        // In the tree: a node leaves the registry on TreeExiting
        Node parent = AutoFree(new Node())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(parent);
        var node = new CounterNode { Value = 1 };
        parent.AddChild(node);
        var id = new NetId(1000);
        _server.GetRequiredService<EntityRegistry>().Register(id, node);
        Tick();
        AssertThat(NetIdsIn(StatePacket(AlicePeer))).Contains(id);
        _connection.Packets.Clear();

        node.Value = 2;
        Online().Remove(HostUid);
        node.Free();
        Tick();

        AssertThat(NetIdsIn(StatePacket(AlicePeer))).ContainsExactly(ServerId<PlayersSessionStorage>());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void StatePacket_TickNumberFollowsTheKind()
    {
        Tick();
        _connection.Packets.Clear();
        Online().Remove(HostUid);
        Tick();

        var reader = new BitReader(StatePacket(AlicePeer));
        AssertThat(reader.ReadBits(8)).IsEqual((ulong) ServerPacketKind.State);
        AssertThat(reader.ReadVarUInt()).IsEqual(2UL);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_UnknownNetId_Throws()
    {
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt(1);
        writer.WriteVarUInt(999);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_TruncatedDelta_Throws()
    {
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt(1);
        writer.WriteVarUInt((ulong) ServerId<PlayersStorage>().Value);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_NoEndOfTheModelsSection_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.State, 1];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_TrailingBytes_Throws()
    {
        // Alice's own client is not attached here, so the packet is not applied twice
        _connection.Receivers.Clear();
        Tick();
        byte[] packet = [..StatePacket(AlicePeer), 0, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(packet));
    }

    // Not a format error in RepliCAT: the delta is well-formed, the client's model just cannot take it
    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_DeltaTheModelCannotTake_Throws()
    {
        var id = new NetId(1000);
        var server = new FixedPartNode(new FixedPartNode.Part { Value = 1 });
        var client = new FixedPartNode();
        _clientNodes.Add(server);
        _clientNodes.Add(client);
        _alice.GetRequiredService<EntityRegistry>().Register(id, client);
        var replicator = new Replicator(NetMessageCodecTests.CreateMapping());
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt(1);
        writer.WriteVarUInt((ulong) id.Value);
        AssertThat(replicator.TryWriteDelta(replicator.CreateBaseline(server), writer)).IsTrue();
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));
    }

    // A failed send leaves the peer behind the baselines for good
    [TestCase]
    [RequireGodotRuntime]
    public void StatePacketSendFailed_DisconnectsOnlyThatPeer()
    {
        JoinDirectly(BobUid, "Bob", BobPeer);
        _connection.FailingPeer = AlicePeer;

        Tick();

        AssertThat(_connection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.State);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_NotAStatePacket_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.Events, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(packet));
    }

    // The host's Simulation has written the state already: nothing in its World applies it
    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_StatePacketInAHostWorld_Throws()
    {
        Tick();
        byte[] packet = StatePacket(AlicePeer);
        GameWorld host = AutoFree(new GameWorld())!.InitPreReady(
            WorldLayer.Host, Dependencies(new RecordingClientsConnection()), WorldOrigin.New);

        AssertThrown(() => host.ReceiveFromServer(packet)).IsInstanceOf<InvalidOperationException>();
    }

    private WorldDependencies Dependencies(RecordingClientsConnection connection) =>
        new(new ManualTimeProvider(Now), _codec, new Replicator(NetMessageCodecTests.CreateMapping()),
            new ManualFrameProvider(), _scenes, TestWorldScenes.CreateCatalog(_scenes), connection, connection);

    // The client gets its storages as 020 will spawn them: the same kinds under the server's NetIds
    private ServiceProvider ClientOf(int peerId)
    {
        var root = new Node();
        _clientNodes.Add(root);
        ServiceProvider client = new WorldServicesBuilder([..GameTypes(), typeof(JoinRecorder)])
            .Build(WorldLayer.Client, Dependencies(new RecordingClientsConnection()), new WorldRoot(root));
        var registry = client.GetRequiredService<EntityRegistry>();
        foreach (Node serverNode in _server.GetRequiredService<IEntityFinder>().GetAll<Node>())
        {
            var node = (Node) Activator.CreateInstance(serverNode.GetType())!;
            _clientNodes.Add(node);
            registry.Register(ServerId(serverNode), node);
        }

        var applier = client.GetRequiredService<StateApplier>();
        var events = client.GetRequiredService<EventDispatcher>();
        _connection.Receivers[peerId] = packet =>
        {
            if (packet.Span[0] == (byte) ServerPacketKind.State)
            {
                applier.ApplyPacket(packet);
            }
            else
            {
                events.DispatchPacket(packet);
            }
        };
        return client;
    }

    // Past the join, whose events would mix with the ones under test
    private void JoinDirectly(string uid, string nick, int peerId)
    {
        Players().AddPlayer(uid).Nick = nick;
        Online().Add(uid);
        _server.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        _server.GetRequiredService<EventOutbox>().AddPeer(peerId);
    }

    private void Join(int peerId, string uid, string nick)
    {
        var join = new JoinRequestCommand(_codec.ProtocolHash, uid, nick, Colors.White);
        _server.GetRequiredService<CommandInbox>().EnqueueFromPeer(peerId, _codec.Encode(join));
    }

    private void Tick() => _server.GetRequiredService<ServerTickLoop>().RunTick();

    private PlayersModel Players() => _server.GetRequiredService<PlayersStorageQuery>().Model;

    private ReplicatedSet<string> Online() =>
        _server.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids;

    private IEnumerable<string> AliceOnline() =>
        _alice.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids.ToList();

    private StateApplier Applier() => _alice.GetRequiredService<StateApplier>();

    private List<ServerPacketKind> Kinds(int peerId) =>
        _connection.Packets
            .Where(sent => sent.PeerId == peerId)
            .Select(sent => (ServerPacketKind) sent.Packet[0])
            .ToList();

    private byte[] StatePacket(int peerId) =>
        _connection.Packets.Single(sent => sent.PeerId == peerId && sent.Packet[0] == (byte) ServerPacketKind.State)
            .Packet;

    private NetId ServerId(Node node) =>
        _server.GetRequiredService<IEntityFinder>().TryGetNetId(node, out NetId id)
            ? id
            : throw new InvalidOperationException($"{node.Name} is not registered on the server");

    private NetId ServerId<T>() where T : Node =>
        ServerId(_server.GetRequiredService<IEntityFinder>().GetSingle<T>());

    // Every delta is applied to a scratch node of its server kind: a delta has no length to skip it by
    private List<NetId> NetIdsIn(byte[] packet)
    {
        var replicator = new Replicator(NetMessageCodecTests.CreateMapping());
        var finder = _server.GetRequiredService<IEntityFinder>();
        var reader = new BitReader(packet);
        reader.ReadBits(8);
        reader.ReadVarUInt();
        List<NetId> ids = [];
        for (ulong id = reader.ReadVarUInt(); id != 0; id = reader.ReadVarUInt())
        {
            ids.Add(new NetId((long) id));
            var scratch = (Node) Activator.CreateInstance(finder.GetNode(ids[^1]).GetType())!;
            try
            {
                replicator.Apply(scratch, ref reader);
            }
            finally
            {
                scratch.Free();
            }
        }
        return ids;
    }

    private static Type[] GameTypes() => typeof(WorldServicesBuilder).Assembly.GetTypes();

    // What Alice's client models hold at the moment her Presentation handles a join
    [Presentation]
    private class JoinRecorder(PlayersStorageQuery players, PlayersSessionStorageQuery session)
    {
        public record Sight(string Uid, bool Online, string? Nick);

        public List<Sight> Seen { get; } = [];

        [EventHandler]
        private void Handle(PlayerJoinedEvent @event)
        {
            bool known = players.Model.PlayerByUid.TryGetValue(@event.Uid, out PlayerModel? player);
            Seen.Add(new Sight(@event.Uid, session.Model.OnlinePlayerUids.Contains(@event.Uid),
                known ? player!.Nick : null));
        }
    }
}

// The client's part is fixed at construction: a delta that brings one where the client has none needs a setter
public partial class FixedPartNode : Node
{
    public class Part
    {
        [Replicated] public int Value;
    }

    [Replicated] public readonly Part? Fixed;

    public FixedPartNode()
    {
    }

    public FixedPartNode(Part part) => Fixed = part;
}

public partial class CounterNode : Node
{
    [Replicated] public int Value;
}
