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
using NeonWarfare.Scenes.Worlds.Ports;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds;

// The World root as Game drives it: the fake transport loops the host's own packets back into the entry points
[TestSuite]
public class WorldEntryPointsTests
{

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const string HostUid = "HostHostHo-Hhhhhhhhhh";
    private const int RemotePeer = 2;
    private const string RemoteUid = "RemoteRemo-Rrrrrrrrrr";
    private const string SaveFileName = "save";

    private static readonly LocalPlayer HostPlayer = new(HostUid, "Host", Colors.White);
    private static readonly LocalPlayer RemotePlayer = new(RemoteUid, "Remote", Colors.White);

    private NetMessageCodec _codec = null!;
    private RecordingClientsConnection _connection = null!;
    private RecordingSaveFiles _saveFiles = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _saveFiles = new RecordingSaveFiles();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ThroughEntryPoints_ReachesPresentationThroughLoopback()
    {
        World world = HostWorld();

        JoinHost(world);

        AssertThat(world.Get<ChatPresentation>().Entries)
            .ContainsExactly(new ChatPresentation.LocalizedServerEntry(Now, "HUD__CHAT_PLAYER_JOINED", ["Host"]));
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
            new ChatPresentation.LocalizedServerEntry(Now, "HUD__CHAT_PLAYER_JOINED", ["Host"]),
            new ChatPresentation.PlayerMessageEntry(Now, HostUid, "Host", "hello"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_JoinRejected_Throws()
    {
        World world = HostWorld();

        AssertThrown(() => world.ReceiveFromServer(JoinRejectedPacket.Write(JoinRejectReason.InvalidNick)))
            .IsInstanceOf<NetMessageFormatException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_BrokenPacket_Throws()
    {
        World world = HostWorld();

        foreach (byte[] packet in new byte[][] { [], [0] })
        {
            AssertThrown(() => world.ReceiveFromServer(packet)).IsInstanceOf<NetMessageFormatException>();
        }
    }

    // A missing client layer is shown by a dedicated server, a missing server one by a World before its InitPreReady
    [TestCase]
    [RequireGodotRuntime]
    public void EntryPoints_WithoutTheirLayer_Throw()
    {
        World dedicated = CreateWorld(DedicatedSetup());
        World notInitialized = AutoFree(new World())!;

        AssertThrown(() => dedicated.ReceiveFromServer(new byte[] { (byte) ServerPacketKind.Events }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.ReceiveFromClient(HostPeer, new byte[] { 0 }))
            .IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.AddClient(HostPeer)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => notInitialized.RemoveClient(HostPeer)).IsInstanceOf<InvalidOperationException>();
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

        World client = CreateWorld(RemoteClientSetup(), new WorldOrigin.FromSnapshot(RemoteSnapshot()));

        AssertThat(client.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Nick))
            .ContainsExactlyInAnyOrder("Host", "Remote");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReceiveFromServer_SecondSnapshot_Throws()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] snapshot = RemoteSnapshot();
        World client = CreateWorld(RemoteClientSetup(), new WorldOrigin.FromSnapshot(snapshot));

        AssertThrown(() => client.ReceiveFromServer(snapshot)).IsInstanceOf<NetMessageFormatException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSave_HasThePlayersOfTheSave()
    {
        byte[] save = HostSave(_codec);

        World loaded = CreateWorld(DedicatedSetup(), new WorldOrigin.FromSave(save, SaveFileName));

        AssertThat(loaded.Get<PlayersStorageQuery>().Model.PlayerByUid[HostUid].Nick).IsEqual("Host");
    }

    // The single-player game of a save: the host's own player joins the loaded world as it joins a new one
    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOnAHost_TheHostJoinsIt()
    {
        World world = HostWorld(new WorldOrigin.FromSave(HostSave(_codec), SaveFileName));

        JoinHost(world);

        AssertThat(world.Get<ChatPresentation>().Entries)
            .ContainsExactly(new ChatPresentation.LocalizedServerEntry(Now, "HUD__CHAT_PLAYER_JOINED", ["Host"]));
        AssertThat(world.Get<PlayerQuery>().OnlinePlayers().Select(player => player.Nick)).ContainsExactly("Host");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_FromSaveOfAnotherProtocol_Throws()
    {
        byte[] save = HostSave(new NetMessageCodec(NetMessageCodecTests.CreateMapping(), ["Another kind"]));
        World world = AutoFree(new World())!;

        AssertThrown(() => world.InitPreReady(DedicatedSetup(), Dependencies(),
                new WorldOrigin.FromSave(save, SaveFileName)))
            .IsInstanceOf<SaveVersionMismatchException>();
    }

    // The autosave on exit: Quit() and the way back to the menu both take the World out of the tree
    [TestCase]
    [RequireGodotRuntime]
    public void ExitTree_ServerWorld_SavesToItsFile_ALoadedOneToTheFileItCameFrom()
    {
        const string loadedFileName = "loaded";
        World host = InTree(HostWorld());
        JoinHost(host);

        host.GetParent().RemoveChild(host);

        RecordingSaveFiles.Written save = _saveFiles.Files.Single();
        AssertThat(save.FileName).IsEqual(SaveFileName);

        World loaded = InTree(CreateWorld(DedicatedSetup(), new WorldOrigin.FromSave(save.Data, loadedFileName)));
        AssertThat(loaded.Get<PlayersStorageQuery>().Model.PlayerByUid[HostUid].Nick).IsEqual("Host");
        Tick(loaded);
        loaded.GetParent().RemoveChild(loaded);

        AssertThat(_saveFiles.Files.Select(file => file.FileName)).ContainsExactly(SaveFileName, loadedFileName);
    }

    // Given the save files all the same
    [TestCase]
    [RequireGodotRuntime]
    public void ExitTree_ClientWorld_SavesNothing()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        World client = InTree(CreateWorld(RemoteClientSetup(), new WorldOrigin.FromSnapshot(RemoteSnapshot())));

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

    // A loaded save stores the host's player, but it is not online until it joins again
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnAHostFromSave_IsOnlineOnlyAfterItsJoin()
    {
        var owner = new RecordingLocalPlayerOwner();
        World world = HostWorld(new WorldOrigin.FromSave(HostSave(_codec), SaveFileName), owner);
        var localPlayer = world.Get<LocalPlayerPresentation>();

        AssertThrown(() => { _ = localPlayer.Player; }).IsInstanceOf<InvalidOperationException>();
        JoinHost(world);

        AssertThat(owner.JoinedCount).IsEqual(1);
        AssertThat(localPlayer.Player.Nick).IsEqual("Host");
    }

    // Done when: a rejected host join goes to the failure path, Game's JoinRejected, and the UI never appears
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnAHostWithRejectedJoin_IsNeverReported()
    {
        var owner = new RecordingLocalPlayerOwner();
        World world = HostWorld(owner: owner);
        var invalid = new LocalPlayer(HostUid, "", Colors.White);

        world.AddClient(HostPeer);
        ((IServerConnection) _connection).Send(_codec.Encode(invalid.ToJoinRequest(_codec.ProtocolHash)));
        Tick(world);

        AssertThat(_connection.Packets.Select(sent => (sent.PeerId, sent.Packet[0])))
            .ContainsExactly((HostPeer, (byte) ServerPacketKind.JoinRejected));
        AssertThat(owner.JoinedCount).IsEqual(0);
        AssertThrown(() => { _ = world.Get<LocalPlayerPresentation>().Player; })
            .IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnAHost_IsGoneAfterItsLeave()
    {
        World world = HostWorld();
        JoinHost(world);

        world.RemoveClient(HostPeer);
        Tick(world);

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

        World client = CreateWorld(RemoteClientSetup(), new WorldOrigin.FromSnapshot(RemoteSnapshot()));

        PlayerModel player = client.Get<LocalPlayerPresentation>().Player;
        AssertThat(player.Uid).IsEqual(RemoteUid);
        AssertThat(player.Nick).IsEqual("Remote");
    }

    // The snapshot makes the player online, but the UI waits for the events packet of the join tick, as on the host
    [TestCase]
    [RequireGodotRuntime]
    public void LocalPlayer_OnARemoteClient_IsReportedByTheEventsOfItsJoinTick()
    {
        World host = HostWorld();
        JoinHost(host);
        JoinRemote(host);
        byte[] events = _connection.Packets
            .Single(sent => sent.PeerId == RemotePeer && sent.Packet[0] == (byte) ServerPacketKind.Events)
            .Packet;
        var owner = new RecordingLocalPlayerOwner();
        World client = CreateWorld(RemoteClientSetup(), new WorldOrigin.FromSnapshot(RemoteSnapshot()), owner);
        AssertThat(owner.JoinedCount).IsEqual(0);

        client.ReceiveFromServer(events);

        AssertThat(owner.JoinedCount).IsEqual(1);
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
        World world = CreateWorld(HostSetup(), origin, owner);
        _connection.Loopback = packet => world.ReceiveFromServer(packet);
        _connection.CommandLoopback = packet => world.ReceiveFromClient(HostPeer, packet);
        return world;
    }

    private World CreateWorld(WorldSetup setup, WorldOrigin? origin = null, ILocalPlayerOwner? owner = null) =>
        AutoFree(new World())!.InitPreReady(
            setup, Dependencies(owner: owner), origin ?? new WorldOrigin.NewWorld(SaveFileName));

    private WorldSetup.Host HostSetup() => TestWorldSetups.Host(HostPlayer, _saveFiles);

    // A client World here is always the remote player's, made from its join snapshot
    private static WorldSetup.RemoteClient RemoteClientSetup() => TestWorldSetups.RemoteClient(RemotePlayer);

    private WorldSetup.Dedicated DedicatedSetup() => TestWorldSetups.Dedicated(saveFiles: _saveFiles);

    private WorldDependencies Dependencies(NetMessageCodec? codec = null, ILocalPlayerOwner? owner = null)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        return new WorldDependencies(
            new ManualTimeProvider(Now), codec ?? _codec,
            new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), scenes,
            TestWorldScenes.CreateCatalog(scenes), _connection, _connection,
            owner ?? new RecordingLocalPlayerOwner());
    }

    // The World hands out no SaveWriter yet: the save comes from a host container built the same way
    private byte[] HostSave(NetMessageCodec codec)
    {
        Node root = AutoFree(new Node())!;
        using ServiceProvider host = new WorldServicesBuilder()
            .Build(HostSetup(), Dependencies(codec), new WorldRoot(root));
        host.GetRequiredService<NewWorldSimulationFacade>().Create();
        host.GetRequiredService<PlayersStorageQuery>().Model.AddPlayer(HostUid).Nick = "Host";
        host.GetRequiredService<ServerTickLoop>().RunTick();
        return host.GetRequiredService<SaveWriter>().Write();
    }

    // As Game joins the host's own player
    private void JoinHost(World world)
    {
        world.AddClient(HostPeer);
        ((IServerConnection) _connection).Send(_codec.Encode(HostPlayer.ToJoinRequest(_codec.ProtocolHash)));
        Tick(world);
    }

    // As Game hands a remote peer to the World; the snapshot is recorded, not delivered
    private void JoinRemote(World world)
    {
        world.AddClient(RemotePeer);
        world.ReceiveFromClient(RemotePeer, _codec.Encode(RemotePlayer.ToJoinRequest(_codec.ProtocolHash)));
        Tick(world);
    }

    private byte[] RemoteSnapshot() =>
        _connection.Packets
            .Single(sent => sent.PeerId == RemotePeer && sent.Packet[0] == (byte) ServerPacketKind.Snapshot)
            .Packet;

    private static World InTree(World world)
    {
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);
        return world;
    }

    // Outside the tree the engine never ticks the World
    private static void Tick(World world) =>
        world.GetChildren().OfType<ServerTickNode>().Single()._PhysicsProcess(0);
}
