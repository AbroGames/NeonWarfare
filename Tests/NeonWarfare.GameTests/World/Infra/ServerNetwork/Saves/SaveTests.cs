using System.Buffers.Binary;
using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.GameTests.World.Infra.Replication;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientReplication;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using RepliCAT;
using RepliCAT.Bits;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork.Saves;

// A host through the real composition root writes the save; a dedicated server, built the same way on a root of its
// own, loads it. Every root is in the tree: a despawned node leaves the registry on TreeExiting
[TestSuite]
public class SaveTests
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
    private EntityCatalog _catalog = null!;
    private RecordingClientsConnection _connection = null!;
    private ServiceProvider _server = null!;
    private readonly List<ServiceProvider> _providers = [];
    private readonly List<Node> _roots = [];

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _catalog = new EntityCatalog(_scenes.GetScenesList(),
            [..NetMessageCodecTests.CreateMapping().Types, typeof(CounterNode), typeof(UnmappedPartNode),
                typeof(ManualTextNode)]);
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _server = Build(WorldLayer.Host, _connection, out _);
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();

        JoinDirectly(_server, HostUid, "Host", HostPeer);
        JoinDirectly(_server, AliceUid, "Alice", AlicePeer);
    }

    [AfterTest]
    public void TearDown()
    {
        _providers.ForEach(provider => provider.Dispose());
        _providers.Clear();
        _roots.ForEach(root => root.Free());
        _roots.Clear();
        _scenes.Free();
    }

    // Done when: the round trip restores the models, NextNetId and the tick number
    [TestCase]
    [RequireGodotRuntime]
    public void Load_RestoresTheEntitiesTheirModelsTheNextNetIdAndTheTick()
    {
        var parent = Spawner(_server).SpawnOnRoot<CounterNode>(node => node.Value = 1);
        var child = Spawner(_server).Spawn<CounterNode>(Id(_server, parent), node => node.Value = 3);
        Tick(_server);
        Players(_server).PlayerByUid[AliceUid].Nick = "Renamed";
        parent.Value = 2;
        Tick(_server);
        Tick(_server);

        ServiceProvider loaded = Load(Saver().Write());

        AssertSameEntities(loaded);
        var registry = loaded.GetRequiredService<IEntityFinder>();
        AssertThat(((CounterNode) registry.GetNode(Id(_server, parent))).Value).IsEqual(2);
        AssertThat(((CounterNode) registry.GetNode(Id(_server, child))).Value).IsEqual(3);
        AssertThat(Players(loaded).PlayerByUid[AliceUid].Nick).IsEqual("Renamed");
        AssertThat(Players(loaded).PlayerByUid[HostUid].Nick).IsEqual("Host");
        AssertThat(loaded.GetRequiredService<ServerTickLoop>().CurrentTick).IsEqual(3L);
        long next = _server.GetRequiredService<NetIdGenerator>().NextValue;
        AssertThat(Id(loaded, Spawner(loaded).SpawnOnRoot<CounterNode>()).Value).IsEqual(next);
    }

    // Done when: a [NotSaved] storage comes back empty after load, under the same NetId
    [TestCase]
    [RequireGodotRuntime]
    public void Load_NotSavedStorage_IsThereEmpty()
    {
        Tick(_server);
        AssertThat(Online(_server).ToList()).ContainsExactlyInAnyOrder(HostUid, AliceUid);

        ServiceProvider loaded = Load(Saver().Write());

        var storage = loaded.GetRequiredService<IEntityFinder>().GetSingle<PlayersSessionStorage>();
        AssertThat(Id(loaded, storage)).IsEqual(Id(_server, Single<PlayersSessionStorage>(_server)));
        AssertThat(Online(loaded).ToList()).IsEmpty();
        AssertThat(Players(loaded).PlayerByUid.Keys).ContainsExactlyInAnyOrder(HostUid, AliceUid);
    }

    // Done when: another protocol hash is a clear error, before anything spawns
    [TestCase]
    [RequireGodotRuntime]
    public void Load_AnotherProtocolHash_ThrowsAVersionMismatch_SpawnsNothing()
    {
        Tick(_server);
        byte[] save = Saver().Write();
        ulong foreign = _codec.ProtocolHash ^ 1;
        BinaryPrimitives.WriteUInt64LittleEndian(save, foreign);
        ServiceProvider loaded = Build(WorldLayer.Dedicated, new RecordingClientsConnection(), out _);

        var error = AssertThrows<SaveVersionMismatchException>(
            () => loaded.GetRequiredService<SaveLoader>().Load(save));

        AssertThat(error.SaveHash).IsEqual(foreign);
        AssertThat(error.ExpectedHash).IsEqual(_codec.ProtocolHash);
        AssertThat(error.Message).Contains(foreign.ToString("X16")).Contains(_codec.ProtocolHash.ToString("X16"));
        AssertThat(loaded.GetRequiredService<IEntityFinder>().GetAll<Node>()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Load_TooShortForTheHash_Throws()
    {
        AssertBroken([1, 2, 3, 4]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Load_TrailingBytes_Throws()
    {
        Tick(_server);

        AssertBroken([..Saver().Write(), 0, 0]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Load_NoEndOfTheRecords_Throws()
    {
        BitWriter writer = Header(next: 1, tick: 0);

        AssertBroken(writer.ToArray());
    }

    // The generator would hand that NetId out again
    [TestCase]
    [RequireGodotRuntime]
    public void Load_NetIdNotBelowTheNextNetId_Throws()
    {
        BitWriter writer = Header(next: 5, tick: 0);
        WriteRecord(writer, 5, _catalog.GetKindId(typeof(CounterNode)));
        writer.WriteVarUInt(0);

        AssertBroken(writer.ToArray());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Load_RecordOfAnUnknownKind_Throws()
    {
        BitWriter writer = Header(next: 100, tick: 0);
        WriteRecord(writer, 1, _catalog.Descriptors.Count);
        writer.WriteVarUInt(0);

        AssertBroken(writer.ToArray());
    }

    // A loaded world goes on from where it was: no restore once it has ticked
    [TestCase]
    [RequireGodotRuntime]
    public void Restore_AfterTheFirstTickOrIdOut_Throws()
    {
        Tick(_server);
        Spawner(_server).SpawnOnRoot<CounterNode>();

        AssertThrown(() => _server.GetRequiredService<ServerTickLoop>().Restore(10))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _server.GetRequiredService<NetIdGenerator>().Restore(10))
            .IsInstanceOf<InvalidOperationException>();
    }

    // The save is the state at the end of the tick, as the joining peers get it
    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_IsWrittenAtTheEndOfTheTick()
    {
        Tick(_server);
        List<byte[]> written = [];
        Saver().RequestSave(written.Add, error => throw error);
        Saver().RequestSave(written.Add, error => throw error);

        var counter = Spawner(_server).SpawnOnRoot<CounterNode>(node => node.Value = 1);
        counter.Value = 2;
        AssertThat(written).IsEmpty();
        Tick(_server);

        AssertThat(written).HasSize(2);
        ServiceProvider loaded = Load(written[0]);
        AssertThat(((CounterNode) loaded.GetRequiredService<IEntityFinder>().GetNode(Id(_server, counter))).Value)
            .IsEqual(2);
        AssertThat(loaded.GetRequiredService<ServerTickLoop>().CurrentTick).IsEqual(2L);
        written.Clear();

        Tick(_server);

        AssertThat(written).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_ACallbackThrows_TheOthersStillRun()
    {
        Tick(_server);
        List<byte[]> written = [];
        Saver().RequestSave(_ => throw new InvalidOperationException("callback"), error => throw error);
        Saver().RequestSave(written.Add, error => throw error);

        Tick(_server);

        AssertThat(written).HasSize(1);
    }

    // The tick still sends its events: a failed save costs the players nothing
    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_WriteFailed_GoesToFailed_TheTickGoesOn()
    {
        var node = Spawner(_server).SpawnOnRoot<ManualTextNode>();
        Tick(_server);
        _connection.Packets.Clear();
        // Unmarked, so no delta writes it: only the save, which writes manual members from the live object
        node.Text = new string('x', 70_000);
        List<Exception> failed = [];
        Saver().RequestSave(_ => throw new InvalidOperationException("written"), failed.Add);

        Join(BobPeer, BobUid, "Bob");
        Tick(_server);

        AssertThat(failed).HasSize(1);
        AssertThat(_connection.Packets.Where(sent => sent.PeerId == AlicePeer)
                .Select(sent => (ServerPacketKind) sent.Packet[0]))
            .Contains(ServerPacketKind.Events);
    }

    // A failed delta has reset the baseline, so the entity has no state to save until a delta succeeds: the save fails
    // instead of loading it empty
    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_SavedEntityWhoseDeltaFailed_GoesToFailedUntilADeltaSucceeds()
    {
        Tick(_server);
        var broken = Spawner(_server).SpawnOnRoot<UnmappedPartNode>();
        List<Exception> failed = [];
        Saver().RequestSave(_ => throw new InvalidOperationException("written"), failed.Add);

        Tick(_server);

        AssertThat(failed).HasSize(1);
        broken.Part = new UnmappedPartNode.MappedPart { Value = 7 };
        Tick(_server);
        ServiceProvider loaded = Load(Saver().Write());
        var copy = (UnmappedPartNode) loaded.GetRequiredService<IEntityFinder>().GetNode(Id(_server, broken));
        AssertThat(copy.Part.Value).IsEqual(7);
    }

    // The autosave on exit: the World is leaving the tree, the entities have left the registry, the baselines remain
    [TestCase]
    [RequireGodotRuntime]
    public void Write_AfterTheServerRootLeftTheTree_HasEveryEntity()
    {
        var counter = Spawner(_server).SpawnOnRoot<CounterNode>(node => node.Value = 4);
        Tick(_server);
        NetId counterId = Id(_server, counter);
        int count = _server.GetRequiredService<IEntityFinder>().GetAll<Node>().Count;
        Node root = _roots[0];

        root.GetParent().RemoveChild(root);
        AssertThat(_server.GetRequiredService<IEntityFinder>().GetAll<Node>()).IsEmpty();
        ServiceProvider loaded = Load(Saver().Write());

        var registry = loaded.GetRequiredService<IEntityFinder>();
        AssertThat(registry.GetAll<Node>()).HasSize(count);
        AssertThat(((CounterNode) registry.GetNode(counterId)).Value).IsEqual(4);
        AssertThat(Players(loaded).PlayerByUid.Keys).ContainsExactlyInAnyOrder(HostUid, AliceUid);
    }

    // The loaded entities are spawned ones for the replicator: the first tick has their full state
    [TestCase]
    [RequireGodotRuntime]
    public void RemoteClientJoiningALoadedWorld_GetsTheLoadedEntities()
    {
        var counter = Spawner(_server).SpawnOnRoot<CounterNode>(node => node.Value = 5);
        Tick(_server);
        var connection = new RecordingClientsConnection();
        ServiceProvider loaded = Load(Saver().Write(), connection);
        ServiceProvider bob = Build(WorldLayer.Client, new RecordingClientsConnection(), out _);
        var applier = bob.GetRequiredService<StateApplier>();
        // Only the snapshot: Bob's events are not under test
        connection.Receivers[BobPeer] = packet =>
        {
            if (packet.Span[0] == (byte) ServerPacketKind.Snapshot) applier.ApplySnapshot(packet);
        };
        Tick(loaded);

        loaded.GetRequiredService<CommandInbox>().EnqueueFromPeer(BobPeer,
            _codec.Encode(new JoinRequestCommand(_codec.ProtocolHash, BobUid, "Bob", Colors.White)));
        Tick(loaded);

        var registry = bob.GetRequiredService<IEntityFinder>();
        AssertThat(registry.GetAll<Node>()).HasSize(loaded.GetRequiredService<IEntityFinder>().GetAll<Node>().Count);
        AssertThat(((CounterNode) registry.GetNode(Id(_server, counter))).Value).IsEqual(5);
        AssertThat(bob.GetRequiredService<PlayersStorageQuery>().Model.PlayerByUid[AliceUid].Nick).IsEqual("Alice");
        AssertThat(bob.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids.ToList())
            .ContainsExactly(BobUid);
    }

    private ServiceProvider Build(WorldLayer layers, RecordingClientsConnection connection, out Node root)
    {
        root = new Node();
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(root);
        _roots.Add(root);
        var dependencies = new WorldDependencies(new ManualTimeProvider(Now), _codec,
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), _scenes, _catalog,
            connection, connection, new RecordingSaveFiles(), TestLocalPlayer.For(layers));
        ServiceProvider provider = new WorldServicesBuilder().Build(layers, dependencies, new WorldRoot(root));
        _providers.Add(provider);
        return provider;
    }

    private ServiceProvider Load(byte[] save, RecordingClientsConnection? connection = null)
    {
        ServiceProvider loaded = Build(WorldLayer.Dedicated, connection ?? new RecordingClientsConnection(), out _);
        loaded.GetRequiredService<SaveLoader>().Load(save);
        return loaded;
    }

    // A broken save, not a foreign one
    private void AssertBroken(byte[] save)
    {
        ServiceProvider loaded = Build(WorldLayer.Dedicated, new RecordingClientsConnection(), out _);

        var error = AssertThrows<SaveFormatException>(() => loaded.GetRequiredService<SaveLoader>().Load(save));

        AssertThat(error.GetType()).IsEqual(typeof(SaveFormatException));
    }

    private static T AssertThrows<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T e)
        {
            return e;
        }
        throw new InvalidOperationException($"{typeof(T).Name} expected, nothing thrown");
    }

    private BitWriter Header(long next, long tick)
    {
        var writer = new BitWriter();
        writer.WriteBits(_codec.ProtocolHash, SaveWriter.HashBits);
        writer.WriteVarUInt((ulong) next);
        writer.WriteVarUInt((ulong) tick);
        return writer;
    }

    private static void WriteRecord(BitWriter writer, long id, int kindId)
    {
        writer.WriteVarUInt((ulong) id);
        writer.WriteVarUInt((ulong) kindId);
        writer.WriteVarUInt(0);
        writer.WriteBool(false);
    }

    // The same NetIds on both sides, each of the same kind and under the copy of the same parent
    private void AssertSameEntities(ServiceProvider loaded)
    {
        var server = _server.GetRequiredService<IEntityFinder>();
        var registry = loaded.GetRequiredService<IEntityFinder>();
        IReadOnlyList<Node> serverNodes = server.GetAll<Node>();
        AssertThat(registry.GetAll<Node>()).HasSize(serverNodes.Count);
        foreach (Node node in serverNodes)
        {
            NetId id = Id(_server, node);
            Node copy = registry.GetNode(id);
            AssertThat(registry.GetKindId(id)).IsEqual(server.GetKindId(id));
            AssertThat(ParentId(registry, copy)).IsEqual(ParentId(server, node));
        }
    }

    private static NetId ParentId(IEntityFinder finder, Node node) =>
        finder.TryGetNetId(node.GetParent(), out NetId id) ? id : NetId.None;

    // Past the join, whose events would mix with the ones under test
    private static void JoinDirectly(ServiceProvider server, string uid, string nick, int peerId)
    {
        Players(server).AddPlayer(uid).Nick = nick;
        Online(server).Add(uid);
        server.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        server.GetRequiredService<EventOutbox>().AddPeer(peerId);
    }

    private void Join(int peerId, string uid, string nick)
    {
        var join = new JoinRequestCommand(_codec.ProtocolHash, uid, nick, Colors.White);
        _server.GetRequiredService<CommandInbox>().EnqueueFromPeer(peerId, _codec.Encode(join));
    }

    private static void Tick(ServiceProvider server) => server.GetRequiredService<ServerTickLoop>().RunTick();

    private SaveWriter Saver() => _server.GetRequiredService<SaveWriter>();

    private static EntitySpawner Spawner(ServiceProvider server) => server.GetRequiredService<EntitySpawner>();

    private static PlayersModel Players(ServiceProvider provider) =>
        provider.GetRequiredService<PlayersStorageQuery>().Model;

    private static ReplicatedSet<string> Online(ServiceProvider provider) =>
        provider.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids;

    private static T Single<T>(ServiceProvider provider) where T : class =>
        provider.GetRequiredService<IEntityFinder>().GetSingle<T>();

    private static NetId Id(ServiceProvider provider, Node node) =>
        provider.GetRequiredService<IEntityFinder>().TryGetNetId(node, out NetId id)
            ? id
            : throw new InvalidOperationException($"{node.Name} is not registered");
}
