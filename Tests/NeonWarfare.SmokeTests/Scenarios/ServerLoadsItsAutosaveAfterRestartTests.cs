using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ServerLoadsItsAutosaveAfterRestartTests
{
    private const string SaveFile = "smoke-server-world";

    /// <summary>
    /// The whole lifecycle of a server world across processes. A client hosts it in a child dedicated server; the
    /// client, stopped by SIGTERM, waits for the child, which autosaves once its admin has left. Then a dedicated
    /// server process loads that save, a client joins it over the network, and the server autosaves on SIGTERM.
    /// The child saved into the user:// of its client, so the second server is named after the client to get the
    /// same one.
    /// </summary>
    [Fact]
    public void Server_LoadsItsAutosaveAfterRestart()
    {
        using UserDataDirectories userData = new();
        int port = FreePort.Take();
        GameLaunch host = new(
            "host",
            [
                "--auto-host-dedicated", "--auto-host-dedicated-headless",
                "--auto-host-port", port.ToString(),
                "--auto-host-savefile", SaveFile,
            ],
            [ServerOpened, ConnectedToServer, EnteredWorld])
        {
            ExitMilestones = [Autosaved(SaveFile), ChildServerExited],
        };

        SmokeRun.Run([host], userData: userData);
        SmokeRun.Run(
            [
                Server(port, SaveFile) with
                {
                    Name = host.Name,
                    Milestones = [LoadedSave(SaveFile), ServerOpened],
                    ExitMilestones = [Autosaved(SaveFile)],
                },
                FirstClient(port),
            ],
            userData: userData);
    }
}
