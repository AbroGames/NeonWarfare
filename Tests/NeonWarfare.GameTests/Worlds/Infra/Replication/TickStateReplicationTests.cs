using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.ClientNetwork;
using NeonWarfare.Scenes.Worlds.Infra.ClientReplication;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Replication;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Tick;
using RepliCAT;
using RepliCAT.Bits;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Replication;

// A host and Alice's remote client, both through the real composition root, joined by the fake transport the way
// Game joins them. Alice joins before the first tick, which spawns the storages on her client; Bob, when a test needs
// him, joins through a snapshot. Every World is in the tree: a despawned node leaves the registry on TreeExiting
[TestSuite]
public class TickStateReplicationTests
{
    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;
    private const int BobPeer = 3;
    private const int CarolPeer = 4;
    private const int DavePeer = 5;
    private const string HostUid = "HostHostHo-Hhhhhhhhhh";
    private const string AliceUid = "AliceAlice-Aaaaaaaaaa";
    private const string BobUid = "BobBobBobB-Bbbbbbbbbb";
    private const string CarolUid = "CarolCarol-Cccccccccc";
    private const string DaveUid = "DaveDaveDa-Dddddddddd";

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private EntityCatalog _catalog = null!;
    private Node _serverRoot = null!;
    private Node _aliceRoot = null!;
    private RecordingClientsConnection _connection = null!;
    private ServiceProvider _server = null!;
    private ServiceProvider _alice = null!;
    private readonly List<ServiceProvider> _clients = [];
    private readonly List<Node> _clientNodes = [];

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        // The test kinds after the game's: the same catalog on both sides
        _catalog = new EntityCatalog(_scenes.GetScenesList(),
            [..NetMessageCodecTests.CreateMapping().Types, typeof(CounterNode), typeof(UnmappedPartNode),
                typeof(ManualTextNode)]);
        _serverRoot = InTree(new Node());
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _server = new WorldServicesBuilder()
            .Build(WorldLayer.Host, Dependencies(WorldLayer.Host, _connection), new WorldRoot(_serverRoot));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();
        _connection.Loopback = _server.GetRequiredService<EventDispatcher>().DispatchPacket;

        _alice = ClientOf(AlicePeer, out _aliceRoot);
        JoinDirectly(HostUid, "Host", HostPeer);
        JoinDirectly(AliceUid, "Alice", AlicePeer);
    }

    [AfterTest]
    public void TearDown()
    {
        _clients.ForEach(client => client.Dispose());
        _clients.Clear();
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

    // Done when: in the join tick the joiner receives the snapshot and then its events packet, and no state packet
    [TestCase]
    [RequireGodotRuntime]
    public void PeerJoinedInTheTick_GetsTheSnapshotThenItsEvents_NoStatePacket()
    {
        var counter = Spawner().SpawnOnRoot<CounterNode>(node => node.Value = 1);
        Tick();
        _connection.Packets.Clear();
        ServiceProvider bob = ClientOf(BobPeer, out _);

        counter.Value = 2;
        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.Snapshot, ServerPacketKind.Events);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
        AssertClientHasTheServerEntities(bob);
        AssertThat(((CounterNode) bob.GetRequiredService<EntityRegistry>().GetNode(ServerId(counter))).Value)
            .IsEqual(2);
        AssertThat(bob.GetRequiredService<JoinRecorder>().Seen)
            .ContainsExactly(new JoinRecorder.Sight(BobUid, true, "Bob"));
        _connection.Packets.Clear();

        Players().PlayerByUid[BobUid].Nick = "Robert";
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.State);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State);
        AssertThat(bob.GetRequiredService<PlayersStorageQuery>().Model.PlayerByUid[BobUid].Nick).IsEqual("Robert");
    }

    // Done when: one spawned in this tick is in the snapshot, with the state at the end of the tick
    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotOfTheJoinTick_HasTheEntitiesSpawnedInIt()
    {
        Tick();
        ServiceProvider bob = ClientOf(BobPeer, out _);

        Join(BobPeer, BobUid, "Bob");
        var parent = Spawner().SpawnOnRoot<CounterNode>(node => node.Value = 1);
        var child = Spawner().Spawn<CounterNode>(ServerId(parent), node => node.Value = 3);
        parent.Value = 2;
        Tick();

        var registry = bob.GetRequiredService<EntityRegistry>();
        Node clientParent = registry.GetNode(ServerId(parent));
        AssertThat(((CounterNode) clientParent).Value).IsEqual(2);
        AssertThat(((CounterNode) registry.GetNode(ServerId(child))).Value).IsEqual(3);
        AssertThat(registry.GetNode(ServerId(child)).GetParent()).IsSame(clientParent);
    }

    // Done when: an entity despawned after the last send is still in the snapshot. Kind and parent come from the spawn:
    // the registry has neither any more
    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotBetweenTicks_HasTheEntitiesDespawnedAfterTheLastSend()
    {
        var parent = Spawner().SpawnOnRoot<CounterNode>(node => node.Value = 1);
        var child = Spawner().Spawn<CounterNode>(ServerId(parent), node => node.Value = 3);
        NetId parentId = ServerId(parent), childId = ServerId(child);
        Tick();

        Spawner().Despawn(parent);
        byte[] snapshot = SnapshotPacket();

        AssertThat(_server.GetRequiredService<IEntityFinder>().TryGetNode(parentId, out _)).IsFalse();
        ServiceProvider bob = ClientOf(BobPeer, out _);
        bob.GetRequiredService<StateApplier>().ApplySnapshot(snapshot);
        var registry = bob.GetRequiredService<EntityRegistry>();
        AssertThat(((CounterNode) registry.GetNode(parentId)).Value).IsEqual(1);
        AssertThat(registry.GetNode(childId).GetParent()).IsSame(registry.GetNode(parentId));
        AssertThat(registry.GetKindId(childId)).IsEqual(_catalog.GetKindId(typeof(CounterNode)));
    }

    // It would go into the snapshot without state, and the next state packet would spawn it again
    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotWithASpawnSinceTheLastSend_Throws()
    {
        Tick();
        Spawner().SpawnOnRoot<CounterNode>(node => node.Value = 1);

        AssertThrown(() => _server.GetRequiredService<StateReplicator>().WriteSnapshot(new BitWriter()))
            .IsInstanceOf<InvalidOperationException>();
    }

    // The host's World is the server's own: it has every entity already
    [TestCase]
    [RequireGodotRuntime]
    public void HostJoiningThroughTheLoopback_GetsNoSnapshot()
    {
        _server.GetRequiredService<CommandInbox>().EnqueuePeerDisconnected(HostPeer);
        Tick();
        _connection.Packets.Clear();

        Join(HostPeer, HostUid, "Host");
        Tick();

        AssertThat(Kinds(HostPeer)).ContainsExactly(ServerPacketKind.Events);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
    }

    // Without a consistent snapshot the joiner's models would drift from the next deltas; the peers already in
    // the world are not affected
    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotWriteFailed_RejectsEveryPeerJoinedInTheTick()
    {
        var node = Spawner().SpawnOnRoot<ManualTextNode>();
        Tick();
        _connection.Packets.Clear();
        // Unmarked, so no delta writes it: only the snapshot, which writes manual members from the live object
        node.Text = new string('x', 70_000);

        Join(BobPeer, BobUid, "Bob");
        Join(CarolPeer, CarolUid, "Carol");
        Tick();

        byte[] rejection = [(byte) ServerPacketKind.JoinRejected, (byte) JoinRejectReason.InternalError];
        AssertThat(_connection.Packets.Where(sent => sent.PeerId == BobPeer).Select(sent => sent.Packet))
            .ContainsExactly(rejection);
        AssertThat(_connection.Packets.Where(sent => sent.PeerId == CarolPeer).Select(sent => sent.Packet))
            .ContainsExactly(rejection);
        AssertThat(_connection.Disconnected).ContainsExactlyInAnyOrder(BobPeer, CarolPeer);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
        _connection.Packets.Clear();

        // Until their peer_disconnected they stay joined, and Dave's join is an event for them too
        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(BobPeer)).IsEmpty();
        AssertThat(Kinds(CarolPeer)).IsEmpty();
        AssertThat(Kinds(AlicePeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotSendFailed_DisconnectsOnlyThatPeer_WithoutItsEvents()
    {
        Tick();
        _connection.Packets.Clear();
        _connection.FailingPeer = BobPeer;
        _connection.FailingKind = ServerPacketKind.Snapshot;

        Join(BobPeer, BobUid, "Bob");
        Join(CarolPeer, CarolUid, "Carol");
        Tick();

        AssertThat(_connection.Disconnected).ContainsExactly(BobPeer);
        AssertThat(Kinds(BobPeer)).IsEmpty();
        AssertThat(Kinds(CarolPeer)).ContainsExactly(ServerPacketKind.Snapshot, ServerPacketKind.Events);
        _connection.Packets.Clear();

        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(BobPeer)).IsEmpty();
        AssertThat(Kinds(CarolPeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_NotASnapshot_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.State, 1, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplySnapshot(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_NoEndOfTheRecords_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.Snapshot, 1];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplySnapshot(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_TrailingBytes_Throws()
    {
        Tick();
        byte[] packet = [..SnapshotPacket(), 0, 0];
        ServiceProvider bob = ClientOf(BobPeer, out _);

        NetMessageCodecTests.AssertRejected(() => bob.GetRequiredService<StateApplier>().ApplySnapshot(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_RecordOfAnUnknownKind_Throws()
    {
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.Snapshot, 8);
        writer.WriteVarUInt(1);
        writer.WriteVarUInt(1000);
        writer.WriteVarUInt((ulong) _catalog.Descriptors.Count);
        writer.WriteVarUInt(0);
        writer.WriteBool(false);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplySnapshot(writer.ToArray()));
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
        AssertThat(Parse(StatePacket(AlicePeer)).Models).IsEmpty();
        _connection.Packets.Clear();

        Online().Remove(HostUid);
        Tick();

        StateRecords records = Parse(StatePacket(AlicePeer));
        AssertThat(records.Spawns).IsEmpty();
        AssertThat(records.Models).ContainsExactly(ServerId<PlayersSessionStorage>());
        AssertThat(records.Despawns).IsEmpty();
        AssertThat(AliceOnline()).ContainsExactly(AliceUid);
    }

    // Done when: PlayersStorage and SessionStorage appear on the client with their models
    [TestCase]
    [RequireGodotRuntime]
    public void FirstTick_SpawnsTheStoragesOnTheClient_WithTheirModels()
    {
        Tick();

        AssertThat(Parse(StatePacket(AlicePeer)).Spawns.Select(spawn => spawn.Id))
            .ContainsExactly(ServerId<PlayersStorage>(), ServerId<PlayersSessionStorage>());
        AssertClientCopy<PlayersStorage>();
        AssertClientCopy<PlayersSessionStorage>();
        AssertThat(_alice.GetRequiredService<PlayersStorageQuery>().Model.PlayerByUid[AliceUid].Nick)
            .IsEqual("Alice");
        AssertThat(AliceOnline()).ContainsExactlyInAnyOrder(HostUid, AliceUid);
    }

    // Done when: spawn. The state is the one at the end of the tick, and it is there before the node enters the tree
    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_ReachesTheClientWithTheStateAtTheEndOfTheTick_AppliedBeforeItEntersTheTree()
    {
        Tick();
        var server = Spawner().SpawnOnRoot<CounterNode>(counter => counter.Value = 1);
        server.Value = 2;
        int? valueOnEnter = null;
        _aliceRoot.ChildEnteredTree += child => valueOnEnter = ((CounterNode) child).Value;

        Tick();

        var client = (CounterNode) AliceNode(ServerId(server));
        AssertThat(client.Value).IsEqual(2);
        AssertThat(valueOnEnter).IsEqual(2);
        AssertThat(client.GetParent()).IsSame(_aliceRoot);
        AssertThat(AliceRegistry().GetKindId(ServerId(server))).IsEqual(_catalog.GetKindId(typeof(CounterNode)));
    }

    // Done when: a nested parent. The parent's record comes first, so the child finds it
    [TestCase]
    [RequireGodotRuntime]
    public void SpawnUnderAParentSpawnedInTheSameTick_IsUnderTheClientCopyOfIt()
    {
        Tick();
        var parent = Spawner().SpawnOnRoot<CounterNode>();
        var child = Spawner().Spawn<CounterNode>(ServerId(parent), counter => counter.Value = 5);

        Tick();

        Node clientChild = AliceNode(ServerId(child));
        AssertThat(clientChild.GetParent()).IsSame(AliceNode(ServerId(parent)));
        AssertThat(((CounterNode) clientChild).Value).IsEqual(5);
    }

    // The first delta threw: the record goes without state, and the models section of the next packet brings it all
    [TestCase]
    [RequireGodotRuntime]
    public void SpawnWhoseFirstDeltaFailed_ReachesTheClient_TheStateComesNextTick()
    {
        Tick();
        var broken = Spawner().SpawnOnRoot<UnmappedPartNode>();
        var next = Spawner().SpawnOnRoot<CounterNode>(counter => counter.Value = 3);

        Tick();

        var client = (UnmappedPartNode) AliceNode(ServerId(broken));
        AssertThat(((CounterNode) AliceNode(ServerId(next))).Value).IsEqual(3);
        broken.Part = new UnmappedPartNode.MappedPart { Value = 7 };

        Tick();

        AssertThat(client.Part.Value).IsEqual(7);
    }

    // Done when: despawn
    [TestCase]
    [RequireGodotRuntime]
    public void Despawn_TakesTheEntityWithItsChildrenOutOfTheClient()
    {
        var parent = Spawner().SpawnOnRoot<CounterNode>();
        var child = Spawner().Spawn<CounterNode>(ServerId(parent));
        NetId parentId = ServerId(parent), childId = ServerId(child);
        Tick();
        Node clientParent = AliceNode(parentId);
        _connection.Packets.Clear();

        Spawner().Despawn(parent);
        Tick();

        AssertThat(Parse(StatePacket(AlicePeer)).Despawns).ContainsExactlyInAnyOrder(parentId, childId);
        AssertThat(AliceRegistry().TryGetNode(parentId, out _)).IsFalse();
        AssertThat(AliceRegistry().TryGetNode(childId, out _)).IsFalse();
        AssertThat(_aliceRoot.GetChildren()).NotContains(clientParent);
        AssertThat(AliceRegistry().GetAll<Node>()).HasSize(2);
    }

    // Done when: spawn and despawn in the same tick. No client has heard of it, so there is nothing to tell
    [TestCase]
    [RequireGodotRuntime]
    public void SpawnAndDespawnInTheSameTick_SendNothing()
    {
        Tick();
        _connection.Packets.Clear();

        Spawner().Despawn(Spawner().SpawnOnRoot<CounterNode>(counter => counter.Value = 1));
        Tick();

        AssertThat(_connection.Packets).IsEmpty();
    }

    // Changed, then despawned in the same tick: a delta for it would reach a client that is about to lose it
    [TestCase]
    [RequireGodotRuntime]
    public void EntityDespawnedInTheTick_HasNoDeltaInIt()
    {
        var node = Spawner().SpawnOnRoot<CounterNode>(counter => counter.Value = 1);
        Tick();
        _connection.Packets.Clear();

        node.Value = 2;
        Online().Remove(HostUid);
        Spawner().Despawn(node);
        Tick();

        AssertThat(Parse(StatePacket(AlicePeer)).Models).ContainsExactly(ServerId<PlayersSessionStorage>());
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
        BitWriter writer = PacketStart();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(999);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_TruncatedDelta_Throws()
    {
        Tick();
        BitWriter writer = PacketStart();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt((ulong) ServerId<PlayersStorage>().Value);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_NoEndOfTheModelsSection_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.State, 1, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_NoEndOfTheDespawnsSection_Throws()
    {
        byte[] packet = [(byte) ServerPacketKind.State, 1, 0, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(packet));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_SpawnOfARegisteredNetId_Throws()
    {
        Tick();

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(
            SpawnPacket(ServerId<PlayersStorage>(), _catalog.GetKindId(typeof(PlayersStorage)), NetId.None)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_SpawnOfAnUnknownKind_Throws()
    {
        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(
            SpawnPacket(new NetId(1000), _catalog.Descriptors.Count, NetId.None)));

        AssertThat(AliceRegistry().TryGetNode(new NetId(1000), out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_SpawnUnderAnUnregisteredParent_Throws()
    {
        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(
            SpawnPacket(new NetId(1000), _catalog.GetKindId(typeof(CounterNode)), new NetId(999))));

        AssertThat(_aliceRoot.GetChildCount()).IsEqual(0);
    }

    // Checked before the first removal: a broken despawns section takes nothing out
    [TestCase]
    [RequireGodotRuntime]
    public void ApplyPacket_DespawnOfAnUnregisteredNetId_Throws_RemovesNothing()
    {
        Tick();
        BitWriter writer = PacketStart();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt((ulong) ServerId<PlayersStorage>().Value);
        writer.WriteVarUInt(999);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyPacket(writer.ToArray()));

        AssertClientCopy<PlayersStorage>();
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
        AliceRegistry().Register(id, client, 0);
        var replicator = new Replicator(NetMessageCodecTests.CreateMapping());
        BitWriter writer = PacketStart();
        writer.WriteVarUInt(0);
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
        _connection.FailingKind = ServerPacketKind.State;
        Join(CarolPeer, CarolUid, "Carol");

        Tick();

        AssertThat(_connection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(Kinds(AlicePeer)).IsEmpty();
        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
        _connection.Packets.Clear();

        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(AlicePeer)).IsEmpty();
        AssertThat(Kinds(BobPeer)).ContainsExactly(ServerPacketKind.State, ServerPacketKind.Events);
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
        World host = AutoFree(new World())!.InitPreReady(
            WorldLayer.Host, Dependencies(WorldLayer.Host, new RecordingClientsConnection()),
            new WorldOrigin.NewWorld("save"));

        AssertThrown(() => host.ReceiveFromServer(packet)).IsInstanceOf<InvalidOperationException>();
    }

    private WorldDependencies Dependencies(WorldLayer layers, RecordingClientsConnection connection) =>
        new(new ManualTimeProvider(Now), _codec, new Replicator(NetMessageCodecTests.CreateMapping()),
            new ManualFrameProvider(), _scenes, _catalog, connection, connection, new RecordingSaveFiles(),
            TestWorldDependencies.LocalPlayer(layers), TestWorldDependencies.Admin(layers),
            TestWorldDependencies.DedicatedServerOwner(layers),
            TestWorldDependencies.LocalPlayerOwner(layers));

    private ServiceProvider ClientOf(int peerId, out Node root)
    {
        root = InTree(new Node());
        _clientNodes.Add(root);
        ServiceProvider client = new WorldServicesBuilder([..GameTypes(), typeof(JoinRecorder)])
            .Build(WorldLayer.Client, Dependencies(WorldLayer.Client, new RecordingClientsConnection()),
                new WorldRoot(root));
        _clients.Add(client);

        var applier = client.GetRequiredService<StateApplier>();
        var events = client.GetRequiredService<EventDispatcher>();
        _connection.Receivers[peerId] = packet =>
        {
            switch ((ServerPacketKind) packet.Span[0])
            {
                case ServerPacketKind.State:
                    applier.ApplyPacket(packet);
                    break;
                case ServerPacketKind.Snapshot:
                    applier.ApplySnapshot(packet);
                    break;
                default:
                    events.DispatchPacket(packet);
                    break;
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

    private EntitySpawner Spawner() => _server.GetRequiredService<EntitySpawner>();

    private EntityRegistry AliceRegistry() => _alice.GetRequiredService<EntityRegistry>();

    private Node AliceNode(NetId id) => AliceRegistry().GetNode(id);

    // The same NetIds on both sides, each of the same kind and under the copy of the same parent
    private void AssertClientHasTheServerEntities(ServiceProvider client)
    {
        var server = _server.GetRequiredService<IEntityFinder>();
        var registry = client.GetRequiredService<EntityRegistry>();
        IReadOnlyList<Node> serverNodes = server.GetAll<Node>();
        AssertThat(registry.GetAll<Node>()).HasSize(serverNodes.Count);
        foreach (Node node in serverNodes)
        {
            NetId id = ServerId(node);
            Node copy = registry.GetNode(id);
            AssertThat(registry.GetKindId(id)).IsEqual(server.GetKindId(id));
            NetId parent = server.TryGetNetId(node.GetParent(), out NetId parentId) ? parentId : NetId.None;
            NetId copyParent =
                registry.TryGetNetId(copy.GetParent(), out NetId copyParentId) ? copyParentId : NetId.None;
            AssertThat(copyParent).IsEqual(parent);
        }
    }

    // The join snapshot as the tick loop frames it
    private byte[] SnapshotPacket()
    {
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.Snapshot, 8);
        writer.WriteVarUInt(1);
        _server.GetRequiredService<StateReplicator>().WriteSnapshot(writer);
        return writer.ToArray();
    }

    // The same NetId and kind on both sides, right under the World root
    private void AssertClientCopy<T>() where T : Node
    {
        NetId id = ServerId<T>();
        var client = AliceRegistry().GetSingle<T>();
        AssertThat(AliceRegistry().TryGetNetId(client, out NetId clientId)).IsTrue();
        AssertThat(clientId).IsEqual(id);
        AssertThat(AliceRegistry().GetKindId(id)).IsEqual(_server.GetRequiredService<IEntityFinder>().GetKindId(id));
        AssertThat(client.GetParent()).IsSame(_aliceRoot);
    }

    private static BitWriter PacketStart()
    {
        var writer = new BitWriter();
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt(1);
        return writer;
    }

    // One spawn record without state, then empty models and despawns sections
    private static byte[] SpawnPacket(NetId id, int kindId, NetId parent)
    {
        BitWriter writer = PacketStart();
        writer.WriteVarUInt((ulong) id.Value);
        writer.WriteVarUInt((ulong) kindId);
        writer.WriteVarUInt((ulong) parent.Value);
        writer.WriteBool(false);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);
        return writer.ToArray();
    }

    private static Node InTree(Node node)
    {
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(node);
        return node;
    }

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

    private record SpawnRecord(NetId Id, int KindId, NetId Parent);

    private record StateRecords(List<SpawnRecord> Spawns, List<NetId> Models, List<NetId> Despawns);

    // Every delta is applied to a scratch node of its kind: a delta has no length to skip it by
    private StateRecords Parse(byte[] packet)
    {
        var finder = _server.GetRequiredService<IEntityFinder>();
        var reader = new BitReader(packet);
        reader.ReadBits(8);
        reader.ReadVarUInt();
        var records = new StateRecords([], [], []);
        for (ulong id = reader.ReadVarUInt(); id != 0; id = reader.ReadVarUInt())
        {
            var spawn = new SpawnRecord(
                new NetId((long) id), (int) reader.ReadVarUInt(), new NetId((long) reader.ReadVarUInt()));
            records.Spawns.Add(spawn);
            if (reader.ReadBool()) SkipDelta(spawn.KindId, ref reader);
        }
        for (ulong id = reader.ReadVarUInt(); id != 0; id = reader.ReadVarUInt())
        {
            records.Models.Add(new NetId((long) id));
            SkipDelta(finder.GetKindId(records.Models[^1]), ref reader);
        }
        for (ulong id = reader.ReadVarUInt(); id != 0; id = reader.ReadVarUInt())
        {
            records.Despawns.Add(new NetId((long) id));
        }
        return records;
    }

    private void SkipDelta(int kindId, ref BitReader reader)
    {
        Node scratch = _catalog.Create(kindId);
        try
        {
            new Replicator(NetMessageCodecTests.CreateMapping()).Apply(scratch, ref reader);
        }
        finally
        {
            scratch.Free();
        }
    }

    private static Type[] GameTypes() => typeof(WorldServicesBuilder).Assembly.GetTypes();

    // What Alice's client models hold at the moment her Presentation handles a join
    [Presentation]
    private class JoinRecorder(PlayersStorageQuery players, PlayersSessionStorageQuery session)
    {
        public record Sight(string? Uid, bool Online, string Nick);

        public List<Sight> Seen { get; } = [];

        // The join message carries only the nick: the player is found by it
        [EventHandler]
        private void Handle(LocalizedChatMessageEvent @event)
        {
            if (@event.Key != "HUD__CHAT_PLAYER_JOINED") return;

            string nick = @event.Args[0];
            PlayerModel? player = players.Model.PlayerByUid.Values.SingleOrDefault(player => player.Nick == nick);
            bool online = player != null && session.Model.OnlinePlayerUids.Contains(player.Uid);
            Seen.Add(new Sight(player?.Uid, online, nick));
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

public partial class ManualTextNode : Node
{
    [Replicated(Manual = true)] public string Text = "";
}

// Its first delta throws: the runtime type of the part is not in the type mapping
public partial class UnmappedPartNode : Node
{
    public class MappedPart
    {
        [Replicated] public int Value;
    }

    public class UnmappedPart : MappedPart;

    [Replicated] public MappedPart Part = new UnmappedPart();
}
