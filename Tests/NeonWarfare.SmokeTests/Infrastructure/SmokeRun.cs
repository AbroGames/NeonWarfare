using Xunit;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Runs one scenario: launch the processes one by one, wait for each to reach its milestones, let them
/// all live a little longer, stop everything, and fail with all the problems at once.
/// </summary>
public static class SmokeRun
{
    /// <summary>
    /// How long one process gets to reach all of its milestones. Generous on purpose: the first run
    /// after a clean checkout also imports assets. A healthy start takes a second or two.
    /// </summary>
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long everything keeps running once every milestone is reached, so an error that follows a
    /// successful start — the first frames of a world, a late RPC — still gets caught.
    /// </summary>
    private static readonly TimeSpan Linger = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How much of a process's output goes into the report when it missed a milestone. Such a process
    /// often printed nothing bad at all, and its last lines are the only clue to where it stopped.
    /// </summary>
    private const int TailLineCount = 15;

    /// <summary>
    /// Processes start in the given order, and the next one only after the previous has reached its
    /// milestones. So a client is launched once the server reports that its socket is open, and there
    /// is no race between them. Once a process misses a milestone, the rest are not started.
    /// </summary>
    public static void Run(params GameLaunch[] launches)
    {
        List<GameProcess> processes = [];
        List<(GameProcess Process, string Problem)> startupProblems = [];
        try
        {
            foreach (GameLaunch launch in launches)
            {
                GameProcess process = GameProcess.Start(launch.Name, launch.Arguments);
                processes.Add(process);

                string? problem = ReachMilestones(process, launch.Milestones);
                if (problem is null) continue;

                startupProblems.Add((process, problem));
                break;
            }

            if (startupProblems.Count == 0)
            {
                Thread.Sleep(Linger);
                startupProblems.AddRange(processes
                    .Where(process => process.HasExited)
                    .Select(process => (process,
                        $"exited on its own with code {process.ExitCode} while it should have kept running")));
            }

            // Stopping before scanning is deliberate: the Serilog sink is asynchronous, so the last
            // lines only arrive as the process shuts down.
            foreach (GameProcess process in processes)
            {
                process.Stop();
            }

            Report(processes, startupProblems);
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
    /// Null when the process printed every milestone in time; otherwise what went wrong.
    /// </summary>
    private static string? ReachMilestones(GameProcess process, IReadOnlyList<string> milestones)
    {
        DateTime deadline = DateTime.UtcNow + StartupTimeout;

        foreach (string milestone in milestones)
        {
            if (process.WaitForLine(milestone, deadline)) continue;

            return process.HasExited
                ? $"exited with code {process.ExitCode} before printing '{milestone}'"
                : $"did not print '{milestone}' within {StartupTimeout.TotalSeconds:0} s";
        }

        return null;
    }

    /// <summary>
    /// Collects violations from every process into one list instead of failing on the first, the way
    /// the unit tests do — fixing them one round-trip at a time is the alternative.
    /// </summary>
    private static void Report(
        IReadOnlyList<GameProcess> processes, IReadOnlyList<(GameProcess Process, string Problem)> startupProblems)
    {
        List<string> problems = [];

        foreach ((GameProcess process, string problem) in startupProblems)
        {
            problems.Add($"[{process.Name}] {problem}. Last lines of its output:");
            problems.AddRange(process.Output.TakeLast(TailLineCount).Select(line => $"[{process.Name}]     {line}"));
        }

        foreach (GameProcess process in processes)
        {
            problems.AddRange(OutputScanner.Scan(process));
        }

        if (problems.Count == 0) return;

        Assert.Fail(
            $"The game run has {problems.Count} problem line(s):{Environment.NewLine}" +
            string.Join(Environment.NewLine, problems));
    }
}
