using Xunit;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Runs one scenario: launch every process, let the game live for a while, stop everything, and fail
/// with all the problems at once.
/// </summary>
public static class SmokeRun
{
    /// <summary>
    /// How long the processes are left running. Long enough to get past asset import, world creation
    /// and — in the multiplayer scenario — the client handshake.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Every process starts at once, with no wait for the server to be ready. HostMultiplayerGameStarter
    /// only opens the socket after the world exists and the client does not retry, so a client can lose
    /// the race and report a failed connection. Accepted for now — if it turns out to be flaky, the fix
    /// is to wait for "Started server successfully" on the server's output before launching clients.
    /// </summary>
    public static void Run(params Func<GameProcess>[] launches)
    {
        List<GameProcess> processes = [];
        try
        {
            foreach (Func<GameProcess> launch in launches)
            {
                processes.Add(launch());
            }

            Thread.Sleep(Lifetime);

            // Stopping before scanning is deliberate: the Serilog sink is asynchronous, so the last
            // lines only arrive as the process shuts down.
            foreach (GameProcess process in processes)
            {
                process.Stop();
            }

            Report(processes);
        }
        finally
        {
            foreach (GameProcess process in processes)
            {
                process.Dispose();
            }
        }
    }

    /// <summary>
    /// Collects violations from every process into one list instead of failing on the first, the way
    /// the unit tests do — fixing them one round-trip at a time is the alternative.
    /// </summary>
    private static void Report(IReadOnlyList<GameProcess> processes)
    {
        List<string> problems = [];
        foreach (GameProcess process in processes)
        {
            problems.AddRange(OutputScanner.Scan(process));
        }

        if (problems.Count == 0) return;

        Assert.Fail(
            $"The game logged {problems.Count} problem(s):{Environment.NewLine}" +
            string.Join(Environment.NewLine, problems));
    }
}
