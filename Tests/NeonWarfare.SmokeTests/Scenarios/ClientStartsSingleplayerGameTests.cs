using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ClientStartsSingleplayerGameTests
{
    /// <summary>
    /// Straight into a single-player game: world generation plus the local handshake.
    /// user:// is a fresh directory for every process, so the save it creates never outlives the run.
    /// </summary>
    [Fact]
    public void Client_StartsSingleplayerGame()
    {
        SmokeRun.Run(new GameLaunch("client", ["--auto-start"], [PlayerJoined]));
    }
}
