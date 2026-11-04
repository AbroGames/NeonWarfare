using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using RepliCAT;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World;

// The World root as Game drives it: the fake transport loops the host's own packets back into the entry points
[TestSuite]
public class WorldEntryPointsTests
{

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const string HostUid = "HostHostHo-Hhhhhhhhhh";
    private const int RemotePeer = 2;
    private const string RemoteUid = "RemoteRemo-Rrrrrrrrrr";

    private NetMessageCodec _codec = null!;
    private RecordingClientsConnection _connection = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThroughEntryPoints_ReachesPresentationThroughLoopback()
    {
        GameWorld world = HostWorld();

        JoinHost(world);

        AssertThat(world.Get<ChatPresentation>().Entries)
            .ContainsExactly(new ChatPresentation.PlayerJoinedEntry(Now, HostUid, "Host"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Commands_LoopBackAsPacketFromHostPeer()
    {
        GameWorld world = HostWorld();
        JoinHost(world);

        world.Commands.Send(new SendChatMessageCommand("hello"));
        Tick(world);

        AssertThat(_connection.Commands.Count).IsEqual(2);
        AssertThat(world.Get<ChatPresentation>().Entries).ContainsExactly(
            new ChatPresentation.PlayerJoinedEntry(Now, HostUid, "Host"),
            new ChatPresentation.PlayerMessageEntry(Now, HostUid, "Host", "hello"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_JoinRejected_DoesNotReachTheEventDispatcher()
    {
        GameWorld world = HostWorld();

        world.ReceiveFromServer(new[] { (byte) ServerPacketKind.JoinRejected, (byte) JoinRejectReason.InvalidNick });

        AssertThat(world.Get<ChatPresentation>().Entries).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_BrokenPacket_Throws()
    {
        GameWorld world = HostWorld();

        foreach (byte[] packet in new[]
                 {
                     [], [(byte) 0], [(byte) ServerPacketKind.JoinRejected],
                     new[] { (byte) ServerPacketKind.JoinRejected, (byte) 1, (byte) 1 },
                 })
        {
            AssertThrown(() => world.ReceiveFromServer(packet)).IsInstanceOf<NetMessageFormatException>();
        }
    }

    // A missing client layer is shown by a dedicated server, a missing server one by a World before its InitPreReady
    [TestCase]
    [RequireGodotRuntime]
    public void EntryPoints_WithoutTheirLayer_Throw()
    {
        GameWorld dedicated = World(WorldLayer.Dedicated);
        GameWorld notInitialized = AutoFree(new GameWorld())!;

        AssertThrown(() => dedicated.ReceiveFromServer(new byte[] { (byte) ServerPacketKind.Events }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.ReceiveFromClient(HostPeer, new byte[] { 0 }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.OnClientConnected(HostPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.OnClientDisconnected(HostPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => { _ = dedicated.Commands; }).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSnapshot_HasThePlayersOfTheServer()
    {
        GameWorld host = HostWorld();
        JoinHost(host);
        JoinRemote(host);

        GameWorld client = World(WorldLayer.Client, new WorldOrigin.FromSnapshot(RemoteSnapshot()));

        AssertThat(client.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Nick))
            .ContainsExactlyInAnyOrder("Host", "Remote");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_SecondSnapshot_Throws()
    {
        GameWorld host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] snapshot = RemoteSnapshot();
        GameWorld client = World(WorldLayer.Client, new WorldOrigin.FromSnapshot(snapshot));

        AssertThrown(() => client.ReceiveFromServer(snapshot)).IsInstanceOf<NetMessageFormatException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSave_HasThePlayersOfTheSave()
    {
        byte[] save = HostSave(_codec);

        GameWorld loaded = World(WorldLayer.Dedicated, new WorldOrigin.FromSave(save));

        AssertThat(loaded.Get<PlayersStorageQuery>().Model.PlayerByUid[HostUid].Nick).IsEqual("Host");
    }

    // The single-player game of a save: the host's own player joins the loaded world as it joins a new one
    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOnAHost_TheHostJoinsIt()
    {
        GameWorld world = HostWorld(new WorldOrigin.FromSave(HostSave(_codec)));

        JoinHost(world);

        AssertThat(world.Get<ChatPresentation>().Entries)
            .ContainsExactly(new ChatPresentation.PlayerJoinedEntry(Now, HostUid, "Host"));
        AssertThat(world.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Nick)).ContainsExactly("Host");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOfAnotherProtocol_Throws()
    {
        byte[] save = HostSave(new NetMessageCodec(NetMessageCodecTests.CreateMapping(), ["Another kind"]));
        GameWorld world = AutoFree(new GameWorld())!;

        AssertThrown(() => world.InitPreReady(WorldLayer.Dedicated, Dependencies(),
                new WorldOrigin.FromSave(save)))
            .IsInstanceOf<SaveVersionMismatchException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Get_OnlyQueryAndPresentationOfItsLayers()
    {
        GameWorld host = HostWorld();
        GameWorld dedicated = World(WorldLayer.Dedicated);

        AssertThat(host.Get<PlayerQuery>()).IsNotNull();
        AssertThat(host.Get<ChatPresentation>()).IsNotNull();
        AssertThat(dedicated.Get<PlayerQuery>()).IsNotNull();
        AssertThrown(() => host.Get<ChatSimulation>()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => host.Get<ChatSimulationFacade>()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => host.Get<CommandInbox>()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => host.Get<PlayerCommandSender>()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => host.Get<EventDispatcher>()).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => dedicated.Get<ChatPresentation>()).IsInstanceOf<InvalidOperationException>();
    }

    private GameWorld HostWorld(WorldOrigin? origin = null)
    {
        GameWorld world = World(WorldLayer.Host, origin);
        _connection.Loopback = packet => world.ReceiveFromServer(packet);
        _connection.CommandLoopback = packet => world.ReceiveFromClient(HostPeer, packet);
        return world;
    }

    private GameWorld World(WorldLayer layers, WorldOrigin? origin = null) =>
        AutoFree(new GameWorld())!.InitPreReady(layers, Dependencies(), origin ?? WorldOrigin.New);

    private WorldDependencies Dependencies(NetMessageCodec? codec = null)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new WorldDependencies(
            new ManualTimeProvider(Now), codec ?? _codec,
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), _connection, _connection);
    }

    // The World hands out no SaveWriter yet: the save comes from a host container built the same way
    private byte[] HostSave(NetMessageCodec codec)
    {
        Node root = AutoFree(new Node())!;
        using ServiceProvider host = new WorldServicesBuilder()
            .Build(WorldLayer.Host, Dependencies(codec), new WorldRoot(root));
        host.GetRequiredService<NewWorldSimulationFacade>().Create();
        host.GetRequiredService<PlayersStorageQuery>().Model.AddPlayer(HostUid).Nick = "Host";
        host.GetRequiredService<ServerTickLoop>().RunTick();
        return host.GetRequiredService<SaveWriter>().Write();
    }

    // As Game joins the host's own player
    private void JoinHost(GameWorld world)
    {
        world.OnClientConnected(HostPeer);
        ((IServerConnection) _connection).Send(
            _codec.Encode(new JoinRequestCommand(_codec.ProtocolHash, HostUid, "Host", Colors.White)));
        Tick(world);
    }

    // As Game hands a remote peer to the World; the snapshot is recorded, not delivered
    private void JoinRemote(GameWorld world)
    {
        world.OnClientConnected(RemotePeer);
        world.ReceiveFromClient(RemotePeer,
            _codec.Encode(new JoinRequestCommand(_codec.ProtocolHash, RemoteUid, "Remote", Colors.White)));
        Tick(world);
    }

    private byte[] RemoteSnapshot() =>
        _connection.Packets
            .Single(sent => sent.PeerId == RemotePeer && sent.Packet[0] == (byte) ServerPacketKind.Snapshot)
            .Packet;

    // Outside the tree the engine never ticks the World
    private static void Tick(GameWorld world) =>
        world.GetChildren().OfType<ServerTickNode>().Single()._PhysicsProcess(0);
}
