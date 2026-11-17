using System.Buffers;
using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;
using NeonWarfare.Scripts.GlobalServices.Settings;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Features.Players;

// The real game types through the composition root of a host; entries go in as packets through the real inbox and the
// events are read back from the outbox, as a peer would receive them
[TestSuite]
public class PlayerJoinLeaveTests
{

    private const long Now = 1_700_000_000;
    private const int HostPeer = RecordingClientsConnection.HostPeer;
    private const int AlicePeer = 2;
    private const int BobPeer = 3;
    private const int AliceSecondPeer = 4;
    private const string HostUid = "HostHostHo-Hhhhhhhhhh";
    private const string AliceUid = "AliceAlice-Aaaaaaaaaa";
    private const string BobUid = "BobBobBobB-Bbbbbbbbbb";

    private static readonly HashSet<Type> EventTypes = [typeof(LocalizedChatMessageEvent), typeof(PlayerJoinedEvent)];
    private static readonly Color Pink = new(1, 0.5f, 0.8f);

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private ServiceProvider _provider = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = TestWorldScenes.Create();
        _root = new Node();
        _clientsConnection = new RecordingClientsConnection { LocalPeerId = HostPeer };
        _provider = Build(WorldLayer.Host, HostUid, dedicatedServerOwner: null);
    }

    [AfterTest]
    public void TearDown()
    {
        _provider.Dispose();
        _root.Free();
        _scenes.Free();
    }

    // The joiner gets its own join event too: its buffer exists before the event is published
    [TestCase]
    [RequireGodotRuntime]
    public void Join_StoresThePlayerOnline_EveryoneGetsTheEvent()
    {
        Join(HostPeer, HostUid, "Host");
        Join(AlicePeer, AliceUid, "Alice", Pink);
        Tick();

        PlayerModel alice = Players().PlayerByUid[AliceUid];
        AssertThat(alice.Nick).IsEqual("Alice");
        AssertThat(alice.Color).IsEqual(Pink);
        AssertThat(Online()).ContainsExactlyInAnyOrder(HostUid, AliceUid);
        object[] bothJoined = [..Joined(HostUid, "Host"), ..Joined(AliceUid, "Alice")];
        AssertThat(PeerEvents(HostPeer)).ContainsExactly(bothJoined);
        AssertThat(PeerEvents(AlicePeer)).ContainsExactly(Joined(AliceUid, "Alice"));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_ReturningPlayer_KeepsTheStoredNickAndColor()
    {
        Join(AlicePeer, AliceUid, "Alice", Pink);
        Tick();
        Disconnect(AlicePeer);
        Tick();

        Join(AliceSecondPeer, AliceUid, "Renamed", Colors.White);
        Tick();

        PlayerModel alice = Players().PlayerByUid[AliceUid];
        AssertThat(alice.Nick).IsEqual("Alice");
        AssertThat(alice.Color).IsEqual(Pink);
        AssertThat(PeerEvents(AliceSecondPeer)).ContainsExactly(Joined(AliceUid, "Alice"));
    }

    // Done when: the player with the admin uid is admin after join; another player is not
    [TestCase]
    [RequireGodotRuntime]
    public void Join_AdminUid_IsAdmin_TheOthersAreNot()
    {
        Join(HostPeer, HostUid, "Host");
        Join(AlicePeer, AliceUid, "Alice");
        Tick();

        AssertThat(Players().PlayerByUid[HostUid].IsAdmin).IsTrue();
        AssertThat(Players().PlayerByUid[AliceUid].IsAdmin).IsFalse();
    }

    // The rights may have come from another admin: a join never takes them
    [TestCase]
    [RequireGodotRuntime]
    public void Join_ReturningAdminWithoutTheAdminUid_StaysAdmin()
    {
        Join(AlicePeer, AliceUid, "Alice");
        Tick();
        Disconnect(AlicePeer);
        Tick();
        Players().PlayerByUid[AliceUid].IsAdmin = true;

        Join(AliceSecondPeer, AliceUid, "Alice");
        Tick();

        AssertThat(Players().PlayerByUid[AliceUid].IsAdmin).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Leave_TakesThePlayerOffline_TheOthersGetTheEvent()
    {
        Join(AlicePeer, AliceUid, "Alice");
        Join(BobPeer, BobUid, "Bob");
        Tick();
        DrainAll();

        Disconnect(AlicePeer);
        Tick();

        AssertThat(Online()).ContainsExactly(BobUid);
        AssertThat(Players().PlayerByUid.ContainsKey(AliceUid)).IsTrue();
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(Left("Alice"));
        AssertThat(Outbox().Peers).ContainsExactly(BobPeer);
    }

    // A client that crashed comes back before ENet notices: the old connection is dropped, not the new one
    [TestCase]
    [RequireGodotRuntime]
    public void Join_UidOnlineOnAnotherPeer_DisplacesIt()
    {
        Join(AlicePeer, AliceUid, "Alice");
        Join(BobPeer, BobUid, "Bob");
        Tick();
        DrainAll();

        Join(AliceSecondPeer, AliceUid, "Alice");
        Tick();

        AssertThat(Online()).ContainsExactlyInAnyOrder(AliceUid, BobUid);
        object[] displaced = [Left("Alice"), ..Joined(AliceUid, "Alice")];
        AssertThat(PeerEvents(BobPeer)).ContainsExactly(displaced);
        AssertThat(PeerEvents(AliceSecondPeer)).ContainsExactly(Joined(AliceUid, "Alice"));
        AssertThat(Outbox().Peers).ContainsExactlyInAnyOrder(BobPeer, AliceSecondPeer);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_InvalidFields_AreRejectedWithTheirReason()
    {
        var cases = new (string? Uid, string? Nick, Color Color, JoinRejectReason Reason)[]
        {
            (null, "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("TestPlayer1", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa-Aaaaaaaaa", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa-Aaaaaaaaaaa", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa_Aaaaaaaaaa", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaa1-Aaaaaaaaaa", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa-Aaaaaaaaa-", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa-Aaaaaaaaa ", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            ("Aaaaaaaaaa-Aaaaaaaaaé", "Alice", Colors.White, JoinRejectReason.InvalidUid),
            (AliceUid, null, Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, "Al", Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, new string('a', 26), Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, "Al ice", Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, "Al\tice", Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, "Al​ice", Colors.White, JoinRejectReason.InvalidNick),
            (AliceUid, "Alice", Colors.Black, JoinRejectReason.InvalidColor),
            (AliceUid, "Alice", new Color(2, 2, 2), JoinRejectReason.InvalidColor),
            (AliceUid, "Alice", new Color(1, 1, 1, -1), JoinRejectReason.InvalidColor),
            (AliceUid, "Alice", new Color(float.NaN, 1, 1), JoinRejectReason.InvalidColor),
        };

        int peerId = 10;
        foreach ((string? uid, string? nick, Color color, JoinRejectReason reason) in cases)
        {
            peerId++;
            Join(peerId, uid!, nick!, color);
            Tick();

            AssertThat(Rejections()).ContainsExactly((peerId, reason));
            AssertThat(_clientsConnection.Disconnected).ContainsExactly(peerId);
            AssertThat(Online()).IsEmpty();
            AssertThat(Players().PlayerByUid).IsEmpty();
            _clientsConnection.Packets.Clear();
            _clientsConnection.Disconnected.Clear();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Join_FieldsAtTheLimits_Pass()
    {
        Join(AlicePeer, AliceUid, "Ali", Colors.White);
        Join(BobPeer, BobUid, new string('б', 25), Pink);
        Tick();

        AssertThat(Rejections()).IsEmpty();
        AssertThat(Online()).HasSize(2);
    }

    // The uid rule repeats the client's generator, so the two may drift apart
    [TestCase]
    [RequireGodotRuntime]
    public void Join_GeneratedUids_Pass()
    {
        var generator = new UidGenerator();
        for (int peerId = 10; peerId < 110; peerId++)
        {
            Join(peerId, generator.Generate(), "Alice");
        }
        Tick();

        AssertThat(Rejections()).IsEmpty();
        AssertThat(Online()).HasSize(100);
    }

    // Done when: a child server stops once the client that started it leaves, its admin
    [TestCase]
    [RequireGodotRuntime]
    public void Leave_AdminOfDedicated_IsReportedToTheOwner_AnotherPlayerIsNot()
    {
        _provider.Dispose();
        _clientsConnection = new RecordingClientsConnection();
        var owner = new RecordingDedicatedServerOwner();
        _provider = Build(WorldLayer.Dedicated, AliceUid, owner);
        Join(AlicePeer, AliceUid, "Alice");
        Join(BobPeer, BobUid, "Bob");
        Tick();

        Disconnect(BobPeer);
        Tick();
        AssertThat(owner.AdminLeftCount).IsEqual(0);

        Disconnect(AlicePeer);
        Tick();
        AssertThat(owner.AdminLeftCount).IsEqual(1);
    }

    private ServiceProvider Build(WorldLayer layers, string adminUid, IDedicatedServerOwner? dedicatedServerOwner)
    {
        ServiceProvider provider = new WorldServicesBuilder().Build(
            layers,
            new WorldDependencies(
                new ManualTimeProvider(Now), _codec,
                new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), _scenes,
                TestWorldScenes.CreateCatalog(_scenes), _clientsConnection, _clientsConnection,
                new RecordingSaveFiles(), TestWorldDependencies.LocalPlayer(layers),
                TestWorldDependencies.Admin(layers, adminUid), dedicatedServerOwner,
                TestWorldDependencies.LocalPlayerOwner(layers)),
            new WorldRoot(_root));
        provider.GetRequiredService<NewWorldSimulationFacade>().Create();
        return provider;
    }

    private void Join(int peerId, string uid, string nick, Color? color = null)
    {
        var join = new JoinRequestCommand(_codec.ProtocolHash, uid, nick, color ?? Colors.White);
        _provider.GetRequiredService<CommandInbox>().EnqueueFromPeer(peerId, _codec.Encode(join));
    }

    private void Disconnect(int peerId) => _provider.GetRequiredService<CommandInbox>().EnqueuePeerDisconnected(peerId);

    // The dispatcher alone, not the whole tick: the tick would send the events away before they are read
    private void Tick() => _provider.GetRequiredService<CommandDispatcher>().ProcessAll();

    private PlayersModel Players() => _provider.GetRequiredService<PlayersStorageQuery>().Model;

    private IEnumerable<string> Online() =>
        _provider.GetRequiredService<PlayersSessionStorageQuery>().Model.OnlinePlayerUids.ToList();

    private EventOutbox Outbox() => _provider.GetRequiredService<EventOutbox>();

    private void DrainAll()
    {
        foreach (int peerId in Outbox().Peers)
        {
            PeerEvents(peerId);
        }
    }

    private IReadOnlyList<object> PeerEvents(int peerId)
    {
        var output = new ArrayBufferWriter<byte>();
        Outbox().DrainEvents(peerId, output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }

    private List<(int, JoinRejectReason)> Rejections() =>
        _clientsConnection.Packets
            .Where(sent => sent.Packet[0] == (byte) ServerPacketKind.JoinRejected)
            .Select(sent => (sent.PeerId, (JoinRejectReason) sent.Packet[1]))
            .ToList();

    private static object[] Joined(string uid, string nick) =>
        [new LocalizedChatMessageEvent(Now, "HUD__CHAT_PLAYER_JOINED", [nick]), new PlayerJoinedEvent(uid)];

    private static LocalizedChatMessageEvent Left(string nick) => new(Now, "HUD__CHAT_PLAYER_LEFT", [nick]);
}
