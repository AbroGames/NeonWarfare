using NeonWarfare.SmokeTests.Infrastructure;

namespace NeonWarfare.SmokeTests.Scenarios;

/// <summary>
/// The milestones and the processes the scenarios are made of.
///
/// The scenarios launch the game the way a player would and check that it gets where it was sent and says nothing
/// bad on the way. They cover what the unit tests structurally cannot: a broken node tree, an injection that comes
/// out null, an exception in _Ready, a client that fails to reach the server. Every process runs headless — see
/// Docs/Smoke-testing.md.
/// </summary>
public static class Launches
{
    /// <summary>ClientRootStarter: the client has initialized and is about to open the menu.</summary>
    public const string ClientStarted = "Starting Client...";

    /// <summary>
    /// Network.OpenServer: the server World is loaded and the ENet socket accepts peers. The socket is
    /// open earlier, but it refuses connections until then.
    /// </summary>
    public const string ServerOpened = "Server opened";

    /// <summary>Network: the ENet handshake with the server is done.</summary>
    public const string ConnectedToServer = "Connected to the server successfully";

    /// <summary>
    /// Game: the join snapshot arrived and the client World is built from it.
    /// </summary>
    public const string EnteredWorld = "Entered the world from the join snapshot";

    /// <summary>
    /// PlayerSimulationFacade: the World has joined a player. The single-player game joins its own player
    /// whose uid is generated, so only the prefix is matched.
    /// </summary>
    public const string PlayerJoined = "Player joined: ";

    /// <summary>
    /// PlayerSimulationFacade on the server: the World let the player go, not just ENet the peer. Followed by
    /// <c>nick (uid)</c>.
    /// </summary>
    public const string PlayerLeft = "Player left: ";

    public const string FirstClientName = "client-1";
    public const string FirstClientUid = "SmokeTestA-Aaaaaaaaaa";
    public const string SecondClientName = "client-2";
    public const string SecondClientUid = "SmokeTestB-Bbbbbbbbbb";

    /// <summary>SaveService: the World wrote its save on the way out.</summary>
    public static string Autosaved(string saveFile) => $"Autosaved to '{saveFile}'";

    /// <summary>SaveLoader: the World was filled from the save, not created anew.</summary>
    public static string LoadedSave(string saveFile) => $"Loaded the save '{saveFile}'";

    /// <summary>
    /// ProcessService on a client: it has waited for its child server to exit. The pid is not known in advance, so
    /// only the prefix is matched; the warning of a child that had to be killed starts the same way, but a warning
    /// fails the run anyway.
    /// </summary>
    public const string ChildServerExited = "Dedicated server process ";

    /// <summary>DedicatedServerGameStarter: the ProcessDeadChecker of a child server saw its client die.</summary>
    public static string ParentDied(int pid) => $"Parent process {pid} is dead";

    /// <summary>
    /// Every server of the scenarios has a save name of its own, because on macOS all processes share the real
    /// user:// and the parallel scenarios would otherwise load or overwrite each other's saves.
    /// </summary>
    public static GameLaunch Server(int port, string saveFile) =>
        new("server", ["--server", "--port", port.ToString(), "--savefile", saveFile], [ServerOpened]);

    public static GameLaunch FirstClient(int port) => Client(FirstClientName, FirstClientUid, port);

    public static GameLaunch SecondClient(int port) => Client(SecondClientName, SecondClientUid, port);

    /// <summary>
    /// The name doubles as the nickname and must be 3 to 25 characters long, or the server rejects the
    /// join — "client-1" fits. The uid follows the format of the generated ones.
    /// </summary>
    private static GameLaunch Client(string name, string uid, int port) => new(
        name,
        [
            "--auto-connect",
            "--auto-connect-ip", "127.0.0.1",
            "--auto-connect-port", port.ToString(),
            "--uid", uid,
            "--nick", name,
        ],
        [ConnectedToServer, EnteredWorld]);
}
