using System.Buffers;
using GdUnit4;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using NeonWarfare.Scenes.World.Simulations;
using NeonWarfare.Scenes.World.Simulations.ChatCommands;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Simulations;

// The real game types through the composition root of a dedicated server; commands go in through the real inbox and
// the events are read back from the outbox, as a peer would receive them
[TestSuite]
public class ChatTests
{
    private const WorldLayer Dedicated =
        WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler | WorldLayer.ServerNetwork
        | WorldLayer.Query;

    private const long Now = 1_700_000_000;
    private const int MaxLength = 1024;
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private static readonly HashSet<Type> EventTypes = [typeof(ChatServerMessageEvent), typeof(ChatPlayerMessageEvent)];

    private NetMessageCodec _codec = null!;
    private WorldPackedScenes _scenes = null!;
    private ServiceProvider _provider = null!;
    private EventOutbox _outbox = null!;
    private PlayerModel _bob = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _scenes = new WorldPackedScenes();
        var persistence = new PersistenceModel();
        var session = new SessionModel();
        _provider = new WorldServicesBuilder().Build(
            Dedicated,
            new WorldDependencies(
                new FixedTime(), persistence, session, _codec, new ManualFrameProvider(), _scenes,
                new RecordingClientsConnection()));
        _outbox = _provider.GetRequiredService<EventOutbox>();

        JoinDirectly(persistence, session, "alice", "Alice", AlicePeer);
        _bob = JoinDirectly(persistence, session, "bob", "Bob", BobPeer);
    }

    [AfterTest]
    public void TearDown()
    {
        _provider.Dispose();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PlayerMessage_ReachesEveryPeer()
    {
        From(AlicePeer, "hello");

        var expected = new ChatPlayerMessageEvent(Now, "alice", "Alice", "hello");
        AssertAll([expected], [expected]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InvalidPlayerMessage_PublishesNothing()
    {
        foreach (string? text in new[]
                 {
                     null, "", "   ", new string('a', MaxLength + 1), "a\tb", "a\0b", "a\u001bb",
                     "a\nb", "a\rb", "a\u2028b", "a\u2029b", "a\u202Eb", "a\u200Bb",
                 })
        {
            From(AlicePeer, text!);

            AssertAll([], []);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PlayerMessageAtTheLimitOrNotAscii_Passes()
    {
        foreach (string text in new[] { new string('a', MaxLength), "привет, 世界 😀" })
        {
            From(AlicePeer, text);

            AssertThat(PeerEvents(BobPeer)).ContainsExactly(new ChatPlayerMessageEvent(Now, "alice", "Alice", text));
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HelpFromPlayer_RepliesOnlyToThem()
    {
        From(AlicePeer, "/help");

        IReadOnlyList<object> alice = PeerEvents(AlicePeer);
        AssertThat(alice.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) alice[0]).Text).Contains("'/help' -> ").NotContains("Admin commands");
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HelpFromAdmin_ListsAdminCommands()
    {
        _bob.IsAdmin = true;

        From(BobPeer, "/help");

        IReadOnlyList<object> bob = PeerEvents(BobPeer);
        AssertThat(bob.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) bob[0]).Text).Contains("'/help' -> ").Contains("Admin commands");
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void UnknownCommand_RepliesNotFoundOnlyToTheSender()
    {
        From(AlicePeer, "/nosuch arg");

        IReadOnlyList<object> alice = PeerEvents(AlicePeer);
        AssertThat(alice.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) alice[0]).Text).Contains("'nosuch' not found");
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    // Hand-made facades below: the game has no admin command yet, and Register's checks need fixture commands

    [TestCase]
    [RequireGodotRuntime]
    public void AdminCommand_RefusedToPlayerRunForAdmin()
    {
        var calls = new List<string>();
        (ChatSimulationFacade facade, EventOutbox outbox, PeerUidMap peers) = HandMade();
        facade.Register([new FixtureCommand("admin", RequiresAdmin: true, calls)]);
        var alice = new PlayerModel("alice") { Nick = "Alice" };
        var root = new PlayerModel("root") { Nick = "Root", IsAdmin = true };
        peers.Bind(alice.Uid, AlicePeer);
        outbox.AddPeer(AlicePeer);
        peers.Bind(root.Uid, BobPeer);
        outbox.AddPeer(BobPeer);

        facade.HandleInput(alice, "/admin x");
        facade.HandleInput(root, "/ADMIN  y ");

        AssertThat(calls).ContainsExactly("root: admin y");
        IReadOnlyList<object> aliceEvents = PeerEvents(outbox, AlicePeer);
        AssertThat(aliceEvents.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) aliceEvents[0]).Text).Contains("requires admin");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_TwiceDuplicateOrBadName_Throws()
    {
        var calls = new List<string>();
        (ChatSimulationFacade twice, _, _) = HandMade();
        twice.Register([]);
        AssertThrown(() => twice.Register([])).IsInstanceOf<InvalidOperationException>();

        foreach (IChatCommand[] commands in new[]
                 {
                     new IChatCommand[] { new FixtureCommand("a", false, calls), new FixtureCommand("a", true, calls) },
                     [new FixtureCommand("", false, calls)],
                     [new FixtureCommand("a b", false, calls)],
                     [new FixtureCommand("Upper", false, calls)],
                 })
        {
            (ChatSimulationFacade facade, _, _) = HandMade();

            AssertThrown(() => facade.Register(commands)).IsInstanceOf<InvalidOperationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Execute_BeforeRegister_Throws()
    {
        (ChatSimulationFacade facade, _, _) = HandMade();

        AssertThrown(() => facade.HandleInput(new PlayerModel("alice"), "/help"))
            .IsInstanceOf<InvalidOperationException>();
    }

    // Stands for the join of task 015
    private PlayerModel JoinDirectly(
        PersistenceModel persistence, SessionModel session, string uid, string nick, int peerId)
    {
        PlayerModel player = persistence.AddPlayer(uid);
        player.Nick = nick;
        session.OnlinePlayerUids.Add(uid);
        _provider.GetRequiredService<PeerUidMap>().Bind(uid, peerId);
        _outbox.AddPeer(peerId);
        return player;
    }

    private void From(int peerId, string text)
    {
        var inbox = _provider.GetRequiredService<CommandInbox>();
        inbox.EnqueueFromPeer(peerId, _codec.Encode(new SendChatMessageCommand(text)));
        _provider.GetRequiredService<CommandDispatcher>().ProcessAll();
    }

    private void AssertAll(object[] alice, object[] bob)
    {
        AssertEvents(PeerEvents(AlicePeer), alice);
        AssertEvents(PeerEvents(BobPeer), bob);
    }

    // ContainsExactly with no arguments passes for any list
    private static void AssertEvents(IReadOnlyList<object> actual, object[] expected)
    {
        AssertThat(actual.Count).IsEqual(expected.Length);
        if (expected.Length > 0)
        {
            AssertThat(actual).ContainsExactly(expected);
        }
    }

    private IReadOnlyList<object> PeerEvents(int peerId) => PeerEvents(_outbox, peerId);

    private IReadOnlyList<object> PeerEvents(EventOutbox outbox, int peerId)
    {
        var output = new ArrayBufferWriter<byte>();
        outbox.DrainEvents(peerId, output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }

    private (ChatSimulationFacade, EventOutbox, PeerUidMap) HandMade()
    {
        var peers = new PeerUidMap();
        var outbox = new EventOutbox(_codec, peers);
        return (new ChatSimulationFacade(new ChatSimulation(new FixedTime(), outbox)), outbox, peers);
    }

    private class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(Now);
    }

    private record FixtureCommand(string Name, bool RequiresAdmin, List<string> Calls) : IChatCommand
    {
        public string Description => "fixture";

        public void Execute(PlayerModel sender, string arguments) => Calls.Add($"{sender.Uid}: {Name} {arguments}");
    }
}
