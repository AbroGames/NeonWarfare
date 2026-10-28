using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Features.Players;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Features.Players;

// The real storages between one server and several clients: a node as the root, PlayerModel created on the client
// through its private constructor and its Uid assigned through a private setter. Default values never get there
[TestSuite]
public class PlayersStorageReplicationTests
{
    private Side _server = null!;
    private ReplicationBaseline _persistenceBaseline = null!;
    private ReplicationBaseline _sessionBaseline = null!;

    [BeforeTest]
    public void SetUp()
    {
        _server = new Side();
        _persistenceBaseline = _server.Replicator.CreateBaseline(_server.Persistence);
        _sessionBaseline = _server.Replicator.CreateBaseline(_server.Session);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Delta_ReachesEveryClient()
    {
        var first = new Side();
        var second = new Side();

        Join("alice", "Alice", Colors.Red, isAdmin: true);
        Join("bob", "Bob", Colors.Blue, isAdmin: false);
        SendDelta(first, second);
        AssertMatchesServer(first);
        AssertMatchesServer(second);

        _server.Persistence.Model.PlayerByUid["bob"].Nick = "Robert";
        _server.Session.Model.OnlinePlayerUids.Remove("alice");
        Join("carol", "Carol", Colors.Green, isAdmin: false);
        SendDelta(first, second);
        AssertMatchesServer(first);
        AssertMatchesServer(second);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Snapshot_ThenDelta_MatchesServer()
    {
        var early = new Side();
        Join("alice", "Alice", Colors.Red, isAdmin: true);
        SendDelta(early);
        _server.Persistence.Model.PlayerByUid["alice"].Nick = "Alicia";
        Join("bob", "Bob", Colors.Blue, isAdmin: false);
        SendDelta(early);

        var late = new Side();
        AssertThat(_server.Replicator.TryWriteSnapshot(_persistenceBaseline, out byte[] persistence)).IsTrue();
        AssertThat(_server.Replicator.TryWriteSnapshot(_sessionBaseline, out byte[] session)).IsTrue();
        late.Replicator.Apply(late.Persistence, persistence);
        late.Replicator.Apply(late.Session, session);
        AssertMatchesServer(late);

        _server.Persistence.Model.PlayerByUid["alice"].Color = Colors.Yellow;
        _server.Session.Model.OnlinePlayerUids.Remove("bob");
        Join("carol", "Carol", Colors.Green, isAdmin: false);
        SendDelta(early, late);
        AssertMatchesServer(early);
        AssertMatchesServer(late);
    }

    private void Join(string uid, string nick, Color color, bool isAdmin)
    {
        PlayerModel player = _server.Persistence.Model.AddPlayer(uid);
        player.Nick = nick;
        player.Color = color;
        player.IsAdmin = isAdmin;
        _server.Session.Model.OnlinePlayerUids.Add(uid);
    }

    // One delta per storage, the same bytes for every client, as at the end of a tick
    private void SendDelta(params Side[] clients)
    {
        bool persistenceChanged = _server.Replicator.TryWriteDelta(_persistenceBaseline, out byte[] persistence);
        bool sessionChanged = _server.Replicator.TryWriteDelta(_sessionBaseline, out byte[] session);
        foreach (Side client in clients)
        {
            if (persistenceChanged)
            {
                client.Replicator.Apply(client.Persistence, persistence);
            }

            if (sessionChanged)
            {
                client.Replicator.Apply(client.Session, session);
            }
        }
    }

    private void AssertMatchesServer(Side client)
    {
        IReadOnlyDictionary<string, PlayerModel> expected = _server.Persistence.Model.PlayerByUid;
        IReadOnlyDictionary<string, PlayerModel> actual = client.Persistence.Model.PlayerByUid;
        AssertThat(actual.Keys.Order()).ContainsExactly(expected.Keys.Order());
        foreach ((string uid, PlayerModel player) in expected)
        {
            PlayerModel received = actual[uid];
            AssertThat(received).IsNotSame(player);
            AssertThat(received.Uid).IsEqual(uid);
            AssertThat(received.Nick).IsEqual(player.Nick);
            AssertThat(received.Color).IsEqual(player.Color);
            AssertThat(received.IsAdmin).IsEqual(player.IsAdmin);
        }

        AssertThat(client.Session.Model.OnlinePlayerUids.Order())
            .ContainsExactly(_server.Session.Model.OnlinePlayerUids.Order());
    }

    // One replicator per process, as in the game
    private sealed class Side
    {
        public readonly Replicator Replicator = new(NetMessageCodecTests.CreateMapping());
        public readonly PlayersStorage Persistence = AutoFree(new PlayersStorage())!;
        public readonly PlayersSessionStorage Session = AutoFree(new PlayersSessionStorage())!;
    }
}
