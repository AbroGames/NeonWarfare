using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Client.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Client.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scripts;
using RepliCAT;
using RepliCAT.Bits;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Replication;

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
    private static readonly PlayerUid HostUid = new("HostHostHo-Hhhhhhhhhh");
    private static readonly PlayerUid AliceUid = new("AliceAlice-Aaaaaaaaaa");
    private static readonly PlayerUid BobUid = new("BobBobBobB-Bbbbbbbbbb");
    private static readonly PlayerUid CarolUid = new("CarolCarol-Cccccccccc");
    private static readonly PlayerUid DaveUid = new("DaveDaveDa-Dddddddddd");

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
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        _scenes = TestWorldScenes.Create();
        // The test kinds after the game's: the same catalog on both sides
        _catalog = new EntityCatalog(_scenes.GetScenesList(),
            [..NetMessageCodecTests.CreateMapping().Types, typeof(CounterNode), typeof(UnmappedPartNode),
                typeof(ManualTextNode), typeof(TimerNode)]);
        _serverRoot = InTree(new Node());
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _server = new WorldServicesBuilder()
            .Build(TestWorldSetups.Host(), Dependencies(_connection), new WorldRoot(_serverRoot));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();
        _connection.Loopback = _server.GetRequiredService<EventDispatcher>().Dispatch;

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
        AssertThat(_alice.GetRequiredService<PlayerQuery>().PlayersModel.PlayerByUid[AliceUid].Nick)
            .IsEqual("Alice");
        Tick();

        AssertThat(_alice.GetRequiredService<PlayerQuery>().PlayersModel.PlayerByUid[AliceUid].Nick)
            .IsEqual("Renamed");
    }

    // Done when: the state section of a tick is applied before the events of the same tick update are handled
    [TestCase]
    [RequireGodotRuntime]
    public void JoinEvent_IsHandledOnAClientWhoseModelsAreAlreadyAtTheEndOfTheTick()
    {
        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(_alice.GetRequiredService<JoinRecorder>().Seen)
            .ContainsExactly(new JoinRecorder.Sight(BobUid, true, "Bob"));
    }

    // Done when: in the join tick the joiner receives the snapshot, then a tick update with its events and no state
    [TestCase]
    [RequireGodotRuntime]
    public void PeerJoinedInTheTick_GetsTheSnapshotThenItsEvents_NoState()
    {
        var counter = Spawner().SpawnOnRoot<CounterNode>(node => node.Value = 1);
        Tick();
        _connection.Packets.Clear();
        ServiceProvider bob = ClientOf(BobPeer, out _);

        counter.Value = 2;
        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(SentKind.Snapshot, SentKind.Events);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(SentKind.State | SentKind.Events);
        AssertClientHasTheServerEntities(bob);
        AssertThat(((CounterNode) bob.GetRequiredService<EntityRegistry>().GetNode(ServerId(counter))).Value)
            .IsEqual(2);
        AssertThat(bob.GetRequiredService<JoinRecorder>().Seen)
            .ContainsExactly(new JoinRecorder.Sight(BobUid, true, "Bob"));
        _connection.Packets.Clear();

        Players().PlayerByUid[BobUid].Nick = "Robert";
        Tick();

        AssertThat(Kinds(BobPeer)).ContainsExactly(SentKind.State);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(SentKind.State);
        AssertThat(bob.GetRequiredService<PlayerQuery>().PlayersModel.PlayerByUid[BobUid].Nick).IsEqual("Robert");
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
        byte[] snapshot = SnapshotRecords();

        AssertThat(_server.GetRequiredService<IEntityFinder>().TryGetNode(parentId, out _)).IsFalse();
        ServiceProvider bob = ClientOf(BobPeer, out _);
        bob.GetRequiredService<StateApplier>().ApplySnapshot(snapshot);
        var registry = bob.GetRequiredService<EntityRegistry>();
        AssertThat(((CounterNode) registry.GetNode(parentId)).Value).IsEqual(1);
        AssertThat(registry.GetNode(childId).GetParent()).IsSame(registry.GetNode(parentId));
        AssertThat(registry.GetKindId(childId)).IsEqual(_catalog.GetKindId(typeof(CounterNode)));
    }

    // It would go into the snapshot without state, and the next state section would spawn it again
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

        AssertThat(Kinds(HostPeer)).ContainsExactly(SentKind.Events);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(SentKind.State | SentKind.Events);
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

        (SentKind, byte) rejection = (SentKind.DisconnectReason, (byte) DisconnectReason.InternalError);
        AssertThat(Sent(BobPeer)).ContainsExactly(rejection);
        AssertThat(Sent(CarolPeer)).ContainsExactly(rejection);
        AssertThat(_connection.Disconnected).ContainsExactlyInAnyOrder(BobPeer, CarolPeer);
        AssertThat(Kinds(AlicePeer)).ContainsExactly(SentKind.State | SentKind.Events);
        _connection.Packets.Clear();

        // Until their peer_disconnected they stay joined, and Dave's join is an event for them too
        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(BobPeer)).IsEmpty();
        AssertThat(Kinds(CarolPeer)).IsEmpty();
        AssertThat(Kinds(AlicePeer)).ContainsExactly(SentKind.State | SentKind.Events);
    }

    // A rejection that does not reach its peer still disconnects it, and costs the other joiners nothing
    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotWriteFailed_FailedRejection_StillRejectsTheOthers()
    {
        var node = Spawner().SpawnOnRoot<ManualTextNode>();
        Tick();
        _connection.Packets.Clear();
        node.Text = new string('x', 70_000);
        _connection.FailingPeer = BobPeer;
        _connection.FailingKind = SentKind.DisconnectReason;

        Join(BobPeer, BobUid, "Bob");
        Join(CarolPeer, CarolUid, "Carol");
        Tick();

        AssertThat(Sent(BobPeer)).IsEmpty();
        AssertThat(Sent(CarolPeer))
            .ContainsExactly((SentKind.DisconnectReason, (byte) DisconnectReason.InternalError));
        AssertThat(_connection.Disconnected).ContainsExactlyInAnyOrder(BobPeer, CarolPeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SnapshotSendFailed_DisconnectsOnlyThatPeer_WithoutItsEvents()
    {
        Tick();
        _connection.Packets.Clear();
        _connection.FailingPeer = BobPeer;
        _connection.FailingKind = SentKind.Snapshot;

        Join(BobPeer, BobUid, "Bob");
        Join(CarolPeer, CarolUid, "Carol");
        Tick();

        AssertThat(_connection.Disconnected).ContainsExactly(BobPeer);
        AssertThat(Sent(BobPeer)).ContainsExactly((SentKind.DisconnectReason, (byte) DisconnectReason.MissedPacket));
        AssertThat(Kinds(CarolPeer)).ContainsExactly(SentKind.Snapshot, SentKind.Events);
        _connection.Packets.Clear();

        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(BobPeer)).IsEmpty();
        AssertThat(Kinds(CarolPeer)).ContainsExactly(SentKind.State | SentKind.Events);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_NoEndOfTheRecords_Throws()
    {
        byte[] records = [];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplySnapshot(records));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_TrailingBytes_Throws()
    {
        Tick();
        byte[] records = [..SnapshotRecords(), 0, 0];
        ServiceProvider bob = ClientOf(BobPeer, out _);

        NetMessageCodecTests.AssertRejected(() => bob.GetRequiredService<StateApplier>().ApplySnapshot(records));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplySnapshot_RecordOfAnUnknownKind_Throws()
    {
        var writer = new BitWriter();
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
    public void StateSection_IsTheSameBytesForEveryPeer_NoneForTheHost()
    {
        JoinDirectly(BobUid, "Bob", BobPeer);

        Tick();

        AssertThat(Kinds(HostPeer)).IsEmpty();
        AssertThat(StateSection(BobPeer)).IsEqual(StateSection(AlicePeer));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickWithoutChanges_SendsNoState()
    {
        Tick();
        _connection.Packets.Clear();

        Tick();

        AssertThat(_connection.Packets).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void StateSection_HasOnlyTheChangedEntities()
    {
        Tick();
        AssertThat(Parse(StateSection(AlicePeer)).Models).IsEmpty();
        _connection.Packets.Clear();

        Online().Remove(HostUid);
        Tick();

        StateRecords records = Parse(StateSection(AlicePeer));
        AssertThat(records.Spawns).IsEmpty();
        AssertThat(records.Models).ContainsExactly(ServerId<OnlinePlayersStorage>());
        AssertThat(records.Despawns).IsEmpty();
        AssertThat(AliceOnline()).ContainsExactly(AliceUid);
    }

    // Done when: PlayersStorage and OnlinePlayersStorage appear on the client with their models
    [TestCase]
    [RequireGodotRuntime]
    public void FirstTick_SpawnsTheStoragesOnTheClient_WithTheirModels()
    {
        Tick();

        AssertThat(Parse(StateSection(AlicePeer)).Spawns.Select(spawn => spawn.Id))
            .ContainsExactly(ServerId<PlayersStorage>(), ServerId<OnlinePlayersStorage>());
        AssertClientCopy<PlayersStorage>();
        AssertClientCopy<OnlinePlayersStorage>();
        AssertThat(_alice.GetRequiredService<PlayerQuery>().PlayersModel.PlayerByUid[AliceUid].Nick)
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

    // The first delta threw: the record goes without state, and the models part of the next section brings it all
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

        AssertThat(Parse(StateSection(AlicePeer)).Despawns).ContainsExactlyInAnyOrder(parentId, childId);
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

        AssertThat(Parse(StateSection(AlicePeer)).Models).ContainsExactly(ServerId<OnlinePlayersStorage>());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickUpdateAndSnapshot_CarryTheTickNumber()
    {
        Tick();
        _connection.Packets.Clear();
        Online().Remove(HostUid);
        Join(BobPeer, BobUid, "Bob");
        Tick();

        AssertThat(_connection.Packets.Select(sent => (sent.PeerId, sent.Kind, sent.Tick))).ContainsExactlyInAnyOrder(
            (AlicePeer, SentKind.State | SentKind.Events, 2L), (BobPeer, SentKind.Snapshot, 2L),
            (BobPeer, SentKind.Events, 2L), (HostPeer, SentKind.Events, 2L));
    }

    // Done when: a timer read on a remote client. It reaches a late joiner in the snapshot, unchanged since its start
    [TestCase]
    [RequireGodotRuntime]
    public void TimerStartedBeforeAJoin_ReadsOnTheJoinerAsOnTheServer()
    {
        var server = Spawner().SpawnOnRoot<TimerNode>();
        Tick();
        server.Timer = TickTimer.Start(_server.GetRequiredService<ServerTickClock>().CurrentTick, 10);
        Tick();
        Tick();
        ServiceProvider bob = ClientOf(BobPeer, out _);

        Join(BobPeer, BobUid, "Bob");
        Tick();

        long serverTick = _server.GetRequiredService<ServerTickClock>().CurrentTick;
        long bobTick = bob.GetRequiredService<ClientTickClock>().CurrentTick;
        TickTimer copy = ((TimerNode) bob.GetRequiredService<EntityRegistry>().GetNode(ServerId(server))).Timer;
        AssertThat(bobTick).IsEqual(serverTick);
        AssertThat(copy).IsEqual(server.Timer);
        AssertThat(copy.Elapsed(bobTick)).IsEqual(3L);
        AssertThat(copy.Remaining(bobTick)).IsEqual(server.Timer.Remaining(serverTick));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_UnknownNetId_Throws()
    {
        BitWriter writer = new BitWriter();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(999);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_TruncatedDelta_Throws()
    {
        Tick();
        BitWriter writer = new BitWriter();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt((ulong) ServerId<PlayersStorage>().Value);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(writer.ToArray()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_NoEndOfTheModelsSection_Throws()
    {
        byte[] section = [0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(section));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_NoEndOfTheDespawnsSection_Throws()
    {
        byte[] section = [0, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(section));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_SpawnOfARegisteredNetId_Throws()
    {
        Tick();

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(
            SpawnPacket(ServerId<PlayersStorage>(), _catalog.GetKindId(typeof(PlayersStorage)), NetId.None)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_SpawnOfAnUnknownKind_Throws()
    {
        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(
            SpawnPacket(new NetId(1000), _catalog.Descriptors.Count, NetId.None)));

        AssertThat(AliceRegistry().TryGetNode(new NetId(1000), out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_SpawnUnderAnUnregisteredParent_Throws()
    {
        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(
            SpawnPacket(new NetId(1000), _catalog.GetKindId(typeof(CounterNode)), new NetId(999))));

        AssertThat(_aliceRoot.GetChildCount()).IsEqual(0);
    }

    // Checked before the first removal: a broken despawns section takes nothing out
    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_DespawnOfAnUnregisteredNetId_Throws_RemovesNothing()
    {
        Tick();
        BitWriter writer = new BitWriter();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);
        writer.WriteVarUInt((ulong) ServerId<PlayersStorage>().Value);
        writer.WriteVarUInt(999);
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(writer.ToArray()));

        AssertClientCopy<PlayersStorage>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_TrailingBytes_Throws()
    {
        // Alice's own client is not attached here, so the section is not applied twice
        _connection.Receivers.Clear();
        Tick();
        byte[] section = [..StateSection(AlicePeer), 0, 0];

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(section));
    }

    // Not a format error in RepliCAT: the delta is well-formed, the client's model just cannot take it
    [TestCase]
    [RequireGodotRuntime]
    public void ApplyState_DeltaTheModelCannotTake_Throws()
    {
        var id = new NetId(1000);
        var server = new FixedPartNode(new FixedPartNode.Part { Value = 1 });
        var client = new FixedPartNode();
        _clientNodes.Add(server);
        _clientNodes.Add(client);
        AliceRegistry().Register(id, client, 0);
        var replicator = NetMessageCodecTests.CreateReplicator();
        BitWriter writer = new BitWriter();
        writer.WriteVarUInt(0);
        writer.WriteVarUInt((ulong) id.Value);
        AssertThat(replicator.TryWriteDelta(replicator.CreateBaseline(server), writer)).IsTrue();
        writer.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => Applier().ApplyState(writer.ToArray()));
    }

    // A failed send leaves the peer behind the baselines for good
    [TestCase]
    [RequireGodotRuntime]
    public void StateSendFailed_DisconnectsOnlyThatPeer()
    {
        JoinDirectly(BobUid, "Bob", BobPeer);
        _connection.FailingPeer = AlicePeer;
        _connection.FailingKind = SentKind.State;
        Join(CarolPeer, CarolUid, "Carol");

        Tick();

        AssertThat(_connection.Disconnected).ContainsExactly(AlicePeer);
        AssertThat(Sent(AlicePeer)).ContainsExactly((SentKind.DisconnectReason, (byte) DisconnectReason.MissedPacket));
        AssertThat(Kinds(BobPeer)).ContainsExactly(SentKind.State | SentKind.Events);
        _connection.Packets.Clear();

        Join(DavePeer, DaveUid, "Dave");
        Tick();

        AssertThat(Kinds(AlicePeer)).IsEmpty();
        AssertThat(Kinds(BobPeer)).ContainsExactly(SentKind.State | SentKind.Events);
    }

    // The host's Simulation has written the state already: nothing in its World applies it
    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveTickUpdateWithState_InAHostWorld_Throws()
    {
        Tick();
        byte[] state = StateSection(AlicePeer);
        World host = AutoFree(new World())!.InitPreReady(
            TestWorldSetups.Host(), Dependencies(new RecordingClientsConnection()),
            new WorldOrigin.NewWorld());

        AssertThrown(() => host.ReceiveTickUpdate(2, state, ReadOnlyMemory<byte>.Empty))
            .IsInstanceOf<InvalidOperationException>();
    }

    private WorldDependencies Dependencies(RecordingClientsConnection connection) =>
        new(new ManualTimeProvider(Now), _codec, TestProtocol.Hash,
            NetMessageCodecTests.CreateReplicator(), new ManualFrameProvider(), _scenes, _catalog,
            connection, connection);

    private ServiceProvider ClientOf(int peerId, out Node root)
    {
        root = InTree(new Node());
        _clientNodes.Add(root);
        ServiceProvider client = new WorldServicesBuilder([..GameTypes(), typeof(JoinRecorder)])
            .Build(TestWorldSetups.RemoteClient(), Dependencies(new RecordingClientsConnection()), new WorldRoot(root));
        _clients.Add(client);

        var clock = client.GetRequiredService<ClientTickClock>();
        var applier = client.GetRequiredService<StateApplier>();
        var events = client.GetRequiredService<EventDispatcher>();
        // In the order of World.IClientEndpoint
        _connection.Receivers[peerId] = sent =>
        {
            clock.Sync(sent.Tick);
            if (sent.Kind == SentKind.Snapshot)
            {
                applier.ApplySnapshot(sent.Body);
                return;
            }
            if (sent.Body.Length > 0) applier.ApplyState(sent.Body);
            if (sent.Events.Length > 0) events.Dispatch(sent.Events);
        };
        return client;
    }

    // Past the join, whose events would mix with the ones under test
    private void JoinDirectly(PlayerUid uid, string nick, int peerId)
    {
        Players().AddPlayer(uid).Nick = nick;
        Online().Add(uid);
        _server.GetRequiredService<PeerGatekeeper>().StartHandshake(peerId);
        _server.GetRequiredService<PeerStateTable>().Join(peerId, uid);
    }

    private void Join(int peerId, PlayerUid uid, string nick)
    {
        _server.GetRequiredService<PeerGatekeeper>().StartHandshake(peerId);
        var join = new JoinRequestCommand(TestProtocol.Hash.Value, uid, nick, Colors.White);
        _server.GetRequiredService<CommandInbox>().EnqueueFromPeer(peerId, _codec.Encode(join));
    }

    private void Tick() => _server.GetRequiredService<ServerTickLoop>().RunTick();

    private PlayersModel Players() => _server.GetRequiredService<PlayerQuery>().PlayersModel;

    private ReplicatedSet<PlayerUid> Online() =>
        _server.GetRequiredService<PlayerQuery>().OnlineModel.Uids;

    private IEnumerable<PlayerUid> AliceOnline() =>
        _alice.GetRequiredService<PlayerQuery>().OnlineModel.Uids.ToList();

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

    // The records of the join snapshot as the tick loop writes them
    private byte[] SnapshotRecords()
    {
        var writer = new BitWriter();
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

    // One spawn record without state, then empty models and despawns parts
    private static byte[] SpawnPacket(NetId id, int kindId, NetId parent)
    {
        BitWriter writer = new BitWriter();
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

    private List<SentKind> Kinds(int peerId) =>
        _connection.Packets
            .Where(sent => sent.PeerId == peerId)
            .Select(sent => sent.Kind)
            .ToList();

    private List<(SentKind, byte)> Sent(int peerId) =>
        _connection.Packets
            .Where(sent => sent.PeerId == peerId)
            .Select(sent => (sent.Kind, sent.Body[0]))
            .ToList();

    private byte[] StateSection(int peerId) =>
        _connection.Packets.Single(sent => sent.PeerId == peerId && sent.Kind.HasFlag(SentKind.State))
            .Body;

    private NetId ServerId(Node node) =>
        _server.GetRequiredService<IEntityFinder>().TryGetNetId(node, out NetId id)
            ? id
            : throw new InvalidOperationException($"{node.Name} is not registered on the server");

    private NetId ServerId<T>() where T : Node =>
        ServerId(_server.GetRequiredService<IEntityFinder>().GetSingle<T>());

    private record SpawnRecord(NetId Id, int KindId, NetId Parent);

    private record StateRecords(List<SpawnRecord> Spawns, List<NetId> Models, List<NetId> Despawns);

    // Every delta is applied to a scratch node of its kind: a delta has no length to skip it by
    private StateRecords Parse(byte[] section)
    {
        var finder = _server.GetRequiredService<IEntityFinder>();
        var reader = new BitReader(section);
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
            NetMessageCodecTests.CreateReplicator().Apply(scratch, ref reader);
        }
        finally
        {
            scratch.Free();
        }
    }

    private static Type[] GameTypes() => typeof(WorldServicesBuilder).Assembly.GetTypes();

    // What Alice's client models hold at the moment her Presentation handles a join
    [Presentation]
    private class JoinRecorder(PlayerQuery players)
        : IEventHandlerOwner
    {
        public record Sight(PlayerUid? Uid, bool Online, string Nick);

        public List<Sight> Seen { get; } = [];

        // The join message carries only the nick: the player is found by it
        [EventHandler]
        private void Handle(LocalizedChatMessageEvent @event)
        {
            if (@event.Text.Key != "HUD__CHAT_PLAYER_JOINED") return;

            string nick = @event.Text.Args[0];
            PlayerModel? player =
                players.PlayersModel.PlayerByUid.Values.SingleOrDefault(player => player.Nick == nick);
            bool online = player != null && players.OnlineModel.Uids.Contains(player.Uid);
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

public partial class TimerNode : Node
{
    [Replicated] public TickTimer Timer;
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
