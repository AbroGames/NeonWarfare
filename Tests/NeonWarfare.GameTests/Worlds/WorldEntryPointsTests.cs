using System.Reflection;
using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Client;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts;
using RepliCAT.Bits;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds;

// The World root as the transports drive it: the fake one loops the host's own packets back into the entry points
[TestSuite]
public class WorldEntryPointsTests
{

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private static readonly PlayerUid HostUid = new("HostHostHo-Hhhhhhhhhh");
    private const int RemotePeer = 2;
    private static readonly PlayerUid RemoteUid = new("RemoteRemo-Rrrrrrrrrr");

    private static readonly LocalPlayer HostPlayer = new(HostUid, "Host", Colors.White);
    private static readonly LocalPlayer RemotePlayer = new(RemoteUid, "Remote", Colors.White);

    private NetMessageCodec _codec = null!;
    private RecordingClientsConnection _connection = null!;
    private RecordingSaveFiles _saveFiles = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _saveFiles = new RecordingSaveFiles();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Send_LoopsBackAsPacketFromHostPeer()
    {
        World world = HostWorld();
        JoinHost(world);

        world.Send(new SendChatMessageCommand("hello"));
        Tick(world);

        AssertThat(_connection.Commands.Count).IsEqual(2);
        AssertThat(world.Get<ChatPresentation>().Entries).ContainsExactly(
            new ChatPresentation.LocalizedServerEntry(Now, new LocalizedText("HUD__CHAT_PLAYER_JOINED", ["Host"])),
            new ChatPresentation.PlayerMessageEntry(Now, HostUid, "Host", "hello"));
    }

    // A missing client layer is shown by a dedicated server, a missing server one by a World before its InitPreReady,
    // a missing client replication by a host
    [TestCase]
    [RequireGodotRuntime]
    public void EntryPoints_WithoutTheirLayer_Throw()
    {
        World dedicated = CreateWorld(DedicatedSetup());
        World host = HostWorld();
        World notInitialized = AutoFree(new World())!;

        AssertThrown(() => dedicated.ReceiveOwnEvents(new byte[] { 0 })).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => host.ReceiveTickUpdate(1, ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty))
            .IsInstanceOf<InvalidOperationException>();

        AssertThrown(() => dedicated.ReceiveTickUpdate(1, new byte[] { 0 }, ReadOnlyMemory<byte>.Empty))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => dedicated.ReceiveTickUpdate(1, ReadOnlyMemory<byte>.Empty, new byte[] { 0 }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.ReceiveFromClient(HostPeer, new byte[] { 0 }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.StartHandshake(HostPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.QueueDisconnection(HostPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => dedicated.Send(new SendChatMessageCommand("hello")))
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSnapshot_HasThePlayersOfTheServer()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);

        World client = CreateWorld(RemoteClientSetup(), RemoteSnapshot());

        AssertThat(client.Get<PlayerQuery>().GetOnlinePlayers().Select(player => player.Nick))
            .ContainsExactlyInAnyOrder("Host", "Remote");
    }

    // The single-player game of a save: the host's own player joins the loaded world as it joins a new one
    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOnAHost_TheHostJoinsIt()
    {
        World world = HostWorld(new WorldOrigin.FromSave(TestWorldSetups.SaveFileName, HostSave(TestProtocol.Hash)));

        JoinHost(world);

        AssertThat(world.Get<ChatPresentation>().Entries)
            .ContainsExactly(new ChatPresentation.LocalizedServerEntry(
                Now, new LocalizedText("HUD__CHAT_PLAYER_JOINED", ["Host"])));
        AssertThat(world.Get<PlayerQuery>().GetOnlinePlayers().Select(player => player.Nick)).ContainsExactly("Host");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOfAnotherProtocol_Throws()
    {
        byte[] save = HostSave(new ProtocolHash(TestProtocol.Hash.Value + 1));
        World world = AutoFree(new World())!;

        AssertThrown(() => world.InitPreReady(DedicatedSetup(), Dependencies(),
                new WorldOrigin.FromSave(TestWorldSetups.SaveFileName, save)))
            .IsInstanceOf<SaveVersionMismatchException>();
    }

    // The autosave on exit: Quit() and the way back to the menu both take the World out of the tree
    [TestCase]
    [RequireGodotRuntime]
    public void ExitTree_ServerWorld_SavesToTheFileOfItsSetup()
    {
        const string loadedFileName = "loaded";
        World host = InTree(HostWorld());
        JoinHost(host);

        host.GetParent().RemoveChild(host);

        RecordingSaveFiles.Written save = _saveFiles.Files.Single();
        AssertThat(save.FileName).IsEqual(TestWorldSetups.SaveFileName);

        World loaded = InTree(
            CreateWorld(DedicatedSetup(loadedFileName), new WorldOrigin.FromSave(save.FileName, save.Data)));
        AssertThat(loaded.Get<PlayerQuery>().PlayersModel.PlayerByUid[HostUid].Nick).IsEqual("Host");
        Tick(loaded);
        loaded.GetParent().RemoveChild(loaded);

        AssertThat(_saveFiles.Files.Select(file => file.FileName))
            .ContainsExactly(TestWorldSetups.SaveFileName, loadedFileName);
    }

    // Given the save files all the same
    [TestCase]
    [RequireGodotRuntime]
    public void ExitTree_ClientWorld_SavesNothing()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        World client = InTree(CreateWorld(RemoteClientSetup(), RemoteSnapshot()));

        client.GetParent().RemoveChild(client);

        AssertThat(client.GetChildren().OfType<SaveOnExitNode>()).IsEmpty();
        AssertThat(_saveFiles.Files).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Get_OnlyQueryAndPresentationOfItsLayers()
    {
        World host = HostWorld();
        World dedicated = CreateWorld(DedicatedSetup());

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

    // Done when: the UI is created by the owner, so "me" is there the moment it is reported, and never before;
    // another player's join is no report
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnAHost_IsReportedOnlineOnlyByItsOwnJoin()
    {
        var owner = new RecordingLocalPlayerOwner();
        World world = HostWorld(owner: owner);
        var localPlayer = world.Get<LocalPlayerPresentation>();
        PlayerModel? playerWhenReported = null;
        owner.OnJoined = () => playerWhenReported = localPlayer.Player;

        AssertThrown(() => { _ = localPlayer.Player; }).IsInstanceOf<InvalidOperationException>();
        JoinHost(world);
        AssertThat(owner.JoinedCount).IsEqual(1);
        AssertThat(playerWhenReported!.Uid).IsEqual(HostUid);
        JoinRemote(world);

        AssertThat(owner.JoinedCount).IsEqual(1);
        AssertThat(localPlayer.Uid).IsEqual(HostUid);
        AssertThat(localPlayer.Player.Uid).IsEqual(HostUid);
    }

    // Done when: a rejected host join goes to the failure path, the transport's JoinRejected, and the UI never appears
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnAHostWithRejectedJoin_IsNeverReported()
    {
        var owner = new RecordingLocalPlayerOwner();
        World world = HostWorld(owner: owner);
        var invalid = new LocalPlayer(HostUid, "", Colors.White);

        world.StartHandshake(HostPeer);
        ((IServerConnection) _connection).Send(_codec.Encode(invalid.ToJoinRequest(TestProtocol.Hash)));
        Tick(world);

        AssertThat(_connection.Packets.Select(sent => (sent.PeerId, sent.Kind)))
            .ContainsExactly((HostPeer, SentKind.JoinRejected));
        AssertThat(owner.JoinedCount).IsEqual(0);
        AssertThrown(() => { _ = world.Get<LocalPlayerPresentation>().Player; })
            .IsInstanceOf<InvalidOperationException>();
    }

    // The client's World is created after the join, from the object the join request was made of
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnARemoteClient_IsThePlayerItJoinedWith()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);

        World client = CreateWorld(RemoteClientSetup(), RemoteSnapshot());

        PlayerModel player = client.Get<LocalPlayerPresentation>().Player;
        AssertThat(player.Uid).IsEqual(RemoteUid);
        AssertThat(player.Nick).IsEqual("Remote");
    }

    // The snapshot makes the player online, but the UI waits for the events of the join tick, as on the host
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnARemoteClient_IsReportedByTheEventsOfItsJoinTick()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] events = RemoteJoinEvents();
        var owner = new RecordingLocalPlayerOwner();
        World client = CreateWorld(RemoteClientSetup(owner), RemoteSnapshot());
        AssertThat(owner.JoinedCount).IsEqual(0);

        client.ReceiveTickUpdate(2, ReadOnlyMemory<byte>.Empty, events);

        AssertThat(owner.JoinedCount).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSnapshot_TheClockIsTheSnapshotTick()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        var snapshot = (WorldOrigin.FromSnapshot) RemoteSnapshot();

        World client = CreateWorld(RemoteClientSetup(), snapshot);

        AssertThat(snapshot.Tick).IsGreater(0);
        AssertThat(Clock(client).CurrentTick).IsEqual(snapshot.Tick);
    }

    // The update is ahead of the snapshot, so only a sync before the events shows its tick to the handler
    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveTickUpdate_AnEventHandlerSeesTheTickOfTheUpdate()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] events = RemoteJoinEvents();
        var owner = new RecordingLocalPlayerOwner();
        World client = CreateWorld(RemoteClientSetup(owner), RemoteSnapshot());
        long updateTick = Clock(client).CurrentTick + 5;
        long? tickWhenReported = null;
        owner.OnJoined = () => tickWhenReported = Clock(client).CurrentTick;

        client.ReceiveTickUpdate(updateTick, ReadOnlyMemory<byte>.Empty, events);

        AssertThat(tickWhenReported).IsEqual(updateTick);
    }

    // Tick takes the single node, so a World with both the server's and the client's would fail here
    [TestCase]
    [RequireGodotRuntime]
    public void TickNode_InEachConfiguration_AdvancesItsClockByOne()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        World[] worlds = [host, CreateWorld(DedicatedSetup()), CreateWorld(RemoteClientSetup(), RemoteSnapshot())];

        foreach (World world in worlds)
        {
            long before = Clock(world).CurrentTick;
            Tick(world);
            AssertThat(Clock(world).CurrentTick).IsEqual(before + 1);
        }
    }

    // The state section is applied first, so a broken one stops the events before any handler runs
    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveTickUpdate_BrokenStateSection_DispatchesNoEvents()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] events = RemoteJoinEvents();
        var owner = new RecordingLocalPlayerOwner();
        World client = CreateWorld(RemoteClientSetup(owner), RemoteSnapshot());
        var state = new BitWriter();
        state.WriteVarUInt(0);
        state.WriteVarUInt(999);
        state.WriteVarUInt(0);
        state.WriteVarUInt(0);

        NetMessageCodecTests.AssertRejected(() => client.ReceiveTickUpdate(2, state.ToArray(), events));

        AssertThat(owner.JoinedCount).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnADedicatedServer_DoesNotExist()
    {
        World dedicated = CreateWorld(DedicatedSetup());

        AssertThrown(() => dedicated.Get<LocalPlayerPresentation>()).IsInstanceOf<InvalidOperationException>();
    }

    private World HostWorld(WorldOrigin? origin = null, ILocalPlayerOwner? owner = null)
    {
        World world = CreateWorld(HostSetup(owner), origin);
        _connection.Loopback = section => world.ReceiveOwnEvents(section);
        _connection.CommandLoopback = packet => world.ReceiveFromClient(HostPeer, packet);
        return world;
    }

    private World CreateWorld(WorldSetup setup, WorldOrigin? origin = null) =>
        AutoFree(new World())!.InitPreReady(setup, Dependencies(), origin ?? new WorldOrigin.NewWorld());

    private WorldSetup.Host HostSetup(ILocalPlayerOwner? owner = null) =>
        TestWorldSetups.Host(HostPlayer, _saveFiles, localPlayerOwner: owner);

    // A client World here is always the remote player's, made from its join snapshot
    private static WorldSetup.RemoteClient RemoteClientSetup(ILocalPlayerOwner? owner = null) =>
        TestWorldSetups.RemoteClient(RemotePlayer, owner);

    private WorldSetup.Dedicated DedicatedSetup(string saveFileName = TestWorldSetups.SaveFileName) =>
        TestWorldSetups.Dedicated(saveFiles: _saveFiles, saveFileName: saveFileName);

    private WorldDependencies Dependencies(ProtocolHash? hash = null)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new WorldDependencies(
            new ManualTimeProvider(Now), _codec, hash ?? TestProtocol.Hash,
            NetMessageCodecTests.CreateReplicator(), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), _connection, _connection);
    }

    // The World hands out no SaveWriter yet: the save comes from a host container built the same way
    private byte[] HostSave(ProtocolHash hash)
    {
        Node root = AutoFree(new Node())!;
        using ServiceProvider host = new WorldServicesBuilder()
            .Build(HostSetup(), Dependencies(hash), new WorldRoot(root));
        host.GetRequiredService<NewWorldSimulationFacade>().Create();
        host.GetRequiredService<PlayerQuery>().PlayersModel.AddPlayer(HostUid).Nick = "Host";
        host.GetRequiredService<ServerTickLoop>().RunTick();
        return host.GetRequiredService<SaveWriter>().Write();
    }

    // As the host's transport joins its own player
    private void JoinHost(World world)
    {
        world.StartHandshake(HostPeer);
        ((IServerConnection) _connection).Send(_codec.Encode(HostPlayer.ToJoinRequest(TestProtocol.Hash)));
        Tick(world);
    }

    // As the server transport hands a remote peer to the World; the snapshot is recorded, not delivered
    private void JoinRemote(World world)
    {
        world.StartHandshake(RemotePeer);
        world.ReceiveFromClient(RemotePeer, _codec.Encode(RemotePlayer.ToJoinRequest(TestProtocol.Hash)));
        Tick(world);
    }

    private WorldOrigin RemoteSnapshot()
    {
        RecordingClientsConnection.Sent snapshot =
            _connection.Packets.Single(sent => sent.PeerId == RemotePeer && sent.Kind == SentKind.Snapshot);
        return new WorldOrigin.FromSnapshot(snapshot.Tick, snapshot.Body);
    }

    // The remote peer's tick update of its join tick carries only events: the snapshot already is the state
    private byte[] RemoteJoinEvents() =>
        _connection.Packets
            .Single(sent => sent.PeerId == RemotePeer && sent.Kind == SentKind.Events)
            .Events;

    private static World InTree(World world)
    {
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);
        return world;
    }

    // Outside the tree the engine never ticks the World
    private static void Tick(World world) =>
        world.GetChildren().OfType<WorldTickNode>().Single()._PhysicsProcess(0);

    // The World hands out no clock, so it is read from the container the way a service takes it
    private static IWorldClock Clock(World world) =>
        ((ServiceProvider) (typeof(World).GetField("_services", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("World has no private field _services"))
            .GetValue(world)!).GetRequiredService<IWorldClock>();
}
