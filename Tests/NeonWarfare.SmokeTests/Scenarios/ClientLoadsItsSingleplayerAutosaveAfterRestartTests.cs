using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ClientLoadsItsSingleplayerAutosaveAfterRestartTests
{
    private const string SaveFile = "smoke-singleplayer-world";

    /// <summary>
    /// A single-player game, where the client itself saves its world on SIGTERM and loads it on the next start.
    /// The second run reuses the user:// of the first.
    /// </summary>
    [Fact]
    public void Client_LoadsItsSingleplayerAutosaveAfterRestart()
    {
        using UserDataDirectories userData = new();
        GameLaunch client = new("client", ["--auto-start", "--auto-start-savefile", SaveFile], [PlayerJoined]);

        SmokeRun.Run([client with { ExitMilestones = [Autosaved(SaveFile)] }], userData: userData);
        SmokeRun.Run([client with { Milestones = [LoadedSave(SaveFile), PlayerJoined] }], userData: userData);
    }
}
