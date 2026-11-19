using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Features.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Presentation;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Features.Saves;

// A host through the real composition root: the command goes in through the real inbox, the tick writes the save to
// the recorded disk, and the reply is read from the events packet a remote peer gets, or from the host's own chat
[TestSuite]
public class SaveCommandTests
{
    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AdminPeer = 2;
    private const int PlayerPeer = 3;
    private const string HostUid = "host";
    private const string AdminUid = "admin";
    private const string PlayerUid = "player";
    private const string FileName = "my save";

    private static readonly HashSet<Type> EventTypes = [typeof(LocalizedChatMessageEvent)];

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private ServiceProvider _server = null!;
    private RecordingClientsConnection _connection = null!;
    private RecordingSaveFiles _files = null!;
    private ManualFrameProvider _frames = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _root = new Node();
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(_root);
        _connection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _files = new RecordingSaveFiles();
        _frames = new ManualFrameProvider();
        _server = new WorldServicesBuilder().Build(
            WorldLayer.Host,
            new WorldDependencies(
                new ManualTimeProvider(Now), _codec,
                new Replicator(NetMessageCodecTests.CreateMapping()), _frames, _scenes,
                TestWorldScenes.CreateCatalog(_scenes), _connection, _connection, _files,
                TestWorldDependencies.LocalPlayer(WorldLayer.Host), TestWorldDependencies.Admin(WorldLayer.Host),
                TestWorldDependencies.DedicatedServerOwner(WorldLayer.Host),
                TestWorldDependencies.LocalPlayerOwner(WorldLayer.Host)),
            new WorldRoot(_root));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();
        _server.GetRequiredService<SaveService>().Init("old");
        _connection.Loopback = _server.GetRequiredService<EventDispatcher>().DispatchPacket;

        JoinDirectly(HostUid, HostPeer).IsAdmin = true;
        JoinDirectly(AdminUid, AdminPeer).IsAdmin = true;
        JoinDirectly(PlayerUid, PlayerPeer);
        Tick();
        _connection.Packets.Clear();
    }

    [AfterTest]
    public void TearDown()
    {
        _server.Dispose();
        _root.Free();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FromAdmin_IsWrittenAndAnswered()
    {
        From(AdminPeer, FileName);

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(FileName);
        AssertThat(_server.GetRequiredService<SaveService>().SaveFileName).IsEqual(FileName);
        AssertThat(Replies(AdminPeer))
            .ContainsExactly(new LocalizedChatMessageEvent(Now, "HUD__CHAT_GAME_SAVED", [FileName]));
        AssertThat(Replies(PlayerPeer)).IsEmpty();
    }

    // What the host's HUD shows: it reads the chat once the mailbox has a notice
    [TestCase]
    [RequireGodotRuntime]
    public void FromHost_DiskFails_ReachesTheHostsHud_ThePreviousFileStays()
    {
        From(HostPeer, FileName);
        RecordingSaveFiles.Written previous = _files.Files.Single();
        _files.Failing = true;
        _frames.Frame++;

        From(HostPeer, FileName);

        AssertThat(_server.GetRequiredService<HudMailbox>().Read<ChatEntryAddedNotice>()).IsNotEmpty();
        AssertThat(_server.GetRequiredService<ChatPresentation>().Entries.Last())
            .IsEqual(new ChatPresentation.LocalizedServerEntry(Now, "HUD__CHAT_GAME_SAVE_FAILED", [FileName]));
        AssertThat(_files.Files).ContainsExactly(previous);
        AssertThat(_server.GetRequiredService<SaveService>().SaveFileName).IsEqual(FileName);
    }

    // The save comes at the end of the tick, after the leave queued behind the command
    [TestCase]
    [RequireGodotRuntime]
    public void FromAdminWhoLeavesInTheTick_IsStillWritten()
    {
        var inbox = _server.GetRequiredService<CommandInbox>();
        inbox.EnqueueFromPeer(AdminPeer, _codec.Encode(new SaveCommand(FileName)));
        inbox.EnqueuePeerDisconnected(AdminPeer);
        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(FileName);
        AssertThat(Replies(AdminPeer)).IsEmpty();
    }

    // Only an admin is shown the save controls, so the command is dropped without a reply
    [TestCase]
    [RequireGodotRuntime]
    public void FromPlayer_IsDropped()
    {
        From(PlayerPeer, FileName);

        AssertThat(_files.Files).IsEmpty();
        AssertThat(Replies(PlayerPeer)).IsEmpty();
    }

    // The rule itself is in SaveFileNameTests
    [TestCase]
    [RequireGodotRuntime]
    public void BadName_IsDropped()
    {
        From(AdminPeer, "../x");

        AssertThat(_files.Files).IsEmpty();
        AssertThat(Replies(AdminPeer)).IsEmpty();
    }

    // Past the join, whose events would mix with the replies
    private PlayerModel JoinDirectly(string uid, int peerId)
    {
        PlayerModel player = _server.GetRequiredService<PlayersStorageQuery>().Model.AddPlayer(uid);
        player.Nick = uid;
        _server.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids.Add(uid);
        _server.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        _server.GetRequiredService<EventOutbox>().AddPeer(peerId);
        return player;
    }

    private void From(int peerId, string fileName)
    {
        _server.GetRequiredService<CommandInbox>().EnqueueFromPeer(peerId, _codec.Encode(new SaveCommand(fileName)));
        Tick();
    }

    private void Tick() => _server.GetRequiredService<ServerTickLoop>().RunTick();

    private List<object> Replies(int peerId) => _connection.Packets
        .Where(sent => sent.PeerId == peerId && sent.Packet[0] == (byte) ServerPacketKind.Events)
        .SelectMany(sent => _codec.ReadSection(sent.Packet.AsMemory(1), EventTypes, out _))
        .ToList();
}
