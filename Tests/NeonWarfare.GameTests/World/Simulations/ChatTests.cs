using System.Buffers;
using GdUnit4;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using NeonWarfare.Scenes.World.Simulations;
using NeonWarfare.Scenes.World.Simulations.ChatCommands;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Simulations;

// The real game types through the composition root of a dedicated server with a window; commands go in through the
// real inputs and the events are read back from the outbox, as a peer and the window would receive them
[TestSuite]
public class ChatTests
{
    private const WorldLayer DedicatedWithServerHud =
        WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler | WorldLayer.ServerNetwork
        | WorldLayer.Query | WorldLayer.ServerHudPresentation | WorldLayer.DedicatedWindow | WorldLayer.ClientNetwork;

    private const long Now = 1_700_000_000;
    private const int MaxLength = 1024;
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    private static readonly HashSet<Type> EventTypes = [typeof(ChatServerMessageEvent), typeof(ChatPlayerMessageEvent)];

    private NetMessageCodec _codec = null!;
    private ServiceProvider _provider = null!;
    private EventOutbox _outbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping());
        var persistence = new PersistenceModel();
        var session = new SessionModel();
        _provider = new WorldServicesBuilder().Build(
            DedicatedWithServerHud,
            new WorldDependencies(new FixedTime(), persistence, session, _codec, new ManualFrameProvider()));
        _outbox = _provider.GetRequiredService<EventOutbox>();

        JoinDirectly(persistence, session, "alice", "Alice", AlicePeer);
        JoinDirectly(persistence, session, "bob", "Bob", BobPeer);
    }

    [AfterTest]
    public void TearDown() => _provider.Dispose();

    [TestCase]
    [RequireGodotRuntime]
    public void PlayerMessage_ReachesEveryPeerAndTheWindow()
    {
        FromAlice("hello");

        var expected = new ChatPlayerMessageEvent(Now, "alice", "Alice", "hello");
        AssertAll([expected], [expected], [expected]);
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
            FromAlice(text!);

            AssertAll([], [], []);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PlayerMessageAtTheLimitOrNotAscii_Passes()
    {
        foreach (string text in new[] { new string('a', MaxLength), "привет, 世界 😀" })
        {
            FromAlice(text);

            AssertThat(PeerEvents(BobPeer)).ContainsExactly(new ChatPlayerMessageEvent(Now, "alice", "Alice", text));
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HelpFromPlayer_RepliesOnlyToThem()
    {
        FromAlice("/help");

        IReadOnlyList<object> alice = PeerEvents(AlicePeer);
        AssertThat(alice.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) alice[0]).Text).Contains("'/help' -> ").NotContains("Admin commands");
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(WindowEvents()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HelpFromWindow_RepliesOnlyToTheWindow()
    {
        FromWindow("/help");

        IReadOnlyList<object> window = WindowEvents();
        AssertThat(window.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) window[0]).Text).Contains("'/help' -> ").Contains("Admin commands");
        AssertThat(PeerEvents(AlicePeer)).IsEmpty();
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WindowMessage_IsAServerMessageToAll()
    {
        FromWindow("restart soon");

        var expected = new ChatServerMessageEvent(Now, "restart soon");
        AssertAll([expected], [expected], [expected]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InvalidWindowMessage_PublishesNothing()
    {
        FromWindow("a\tb");

        AssertAll([], [], []);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void UnknownCommand_RepliesNotFoundOnlyToTheSender()
    {
        FromAlice("/nosuch arg");

        IReadOnlyList<object> alice = PeerEvents(AlicePeer);
        AssertThat(alice.Count).IsEqual(1);
        AssertThat(((ChatServerMessageEvent) alice[0]).Text).Contains("'nosuch' not found");
        AssertThat(PeerEvents(BobPeer)).IsEmpty();
        AssertThat(WindowEvents()).IsEmpty();
    }

    // Hand-made facades below: the game has no admin command yet, and Register's checks need fixture commands

    [TestCase]
    [RequireGodotRuntime]
    public void AdminCommand_RefusedToPlayerRunForWindowAndAdmin()
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

        facade.HandleInputFromPlayer(alice, "/admin x");
        facade.HandleInputFromPlayer(root, "/ADMIN  y ");
        facade.HandleInputFromDedicatedWindow("/admin z");

        AssertThat(calls).ContainsExactly("admin y", "admin z");
        IReadOnlyList<object> aliceEvents = Read(output => outbox.DrainPeerEvents(AlicePeer, output));
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

        AssertThrown(() => facade.HandleInputFromDedicatedWindow("/help")).IsInstanceOf<InvalidOperationException>();
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

    private void FromAlice(string text)
    {
        var inbox = _provider.GetRequiredService<CommandInbox>();
        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(new SendChatMessageCommand(text)));
        _provider.GetRequiredService<CommandDispatcher>().ProcessAll();
    }

    private void FromWindow(string text)
    {
        _provider.GetRequiredService<DedicatedWindowCommandSender>().Send(new SendChatMessageCommand(text));
        _provider.GetRequiredService<CommandDispatcher>().ProcessAll();
    }

    private void AssertAll(object[] alice, object[] bob, object[] window)
    {
        AssertEvents(PeerEvents(AlicePeer), alice);
        AssertEvents(PeerEvents(BobPeer), bob);
        AssertEvents(WindowEvents(), window);
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

    private IReadOnlyList<object> PeerEvents(int peerId) => Read(output => _outbox.DrainPeerEvents(peerId, output));

    private IReadOnlyList<object> WindowEvents() => Read(output => _outbox.DrainDedicatedWindowEvents(output));

    private IReadOnlyList<object> Read(Action<ArrayBufferWriter<byte>> drain)
    {
        var output = new ArrayBufferWriter<byte>();
        drain(output);
        return _codec.ReadSection(output.WrittenMemory, EventTypes, out _);
    }

    private (ChatSimulationFacade, EventOutbox, PeerUidMap) HandMade()
    {
        var peers = new PeerUidMap();
        var outbox = new EventOutbox(_codec, peers);
        outbox.AddDedicatedWindow();
        return (new ChatSimulationFacade(new ChatSimulation(new FixedTime(), outbox)), outbox, peers);
    }

    private class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(Now);
    }

    private record FixtureCommand(string Name, bool RequiresAdmin, List<string> Calls) : IChatCommand
    {
        public string Description => "fixture";

        public void Execute(bool isAdmin, Action<string> reply, string arguments) => Calls.Add($"{Name} {arguments}");
    }
}
