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
    /// How long the witness of a <see cref="Departure"/> gets to notice it. A graceful exit disconnects
    /// the ENet peer on the spot; this only has to outlast a slow machine.
    /// </summary>
    private static readonly TimeSpan DepartureTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the exiter of a <see cref="SelfExit"/> gets to quit. ProcessDeadChecker looks every 5 s, and the
    /// autosave on the way out comes after that.
    /// </summary>
    private static readonly TimeSpan SelfExitTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long everything keeps running once every milestone is reached, so an error that follows a
    /// successful start — the first frames of a world, a late packet — still gets caught.
    /// </summary>
    private static readonly TimeSpan Linger = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How much of a process's output goes into the report when it missed a milestone. Such a process
    /// often printed nothing bad at all, and its last lines are the only clue to where it stopped.
    /// </summary>
    private const int TailLineCount = 15;

    /// <inheritdoc cref="Run(IReadOnlyList{GameLaunch}, Departure?, SelfExit?, UserDataDirectories?)"/>
    public static void Run(params GameLaunch[] launches) => Run(launches, departure: null);

    /// <summary>
    /// Processes start in the given order, and the next one only after the previous has reached its
    /// milestones. So a client is launched once the server reports that its socket is open, and there
    /// is no race between them. Once a process misses a milestone, the rest are not started.
    ///
    /// With a <paramref name="departure"/>, once everything has lingered the leaver is stopped alone,
    /// the witness must print its milestone, and the rest linger once more before they are stopped too —
    /// in launch order, so otherwise it is always the server that goes first.
    ///
    /// With a <paramref name="selfExit"/>, once everything has lingered its cause is run, and the exiter must quit
    /// on its own with exit code 0.
    ///
    /// Without <paramref name="userData"/> every process gets a fresh user:// that is gone after the run.
    /// </summary>
    public static void Run(
        IReadOnlyList<GameLaunch> launches,
        Departure? departure = null,
        SelfExit? selfExit = null,
        UserDataDirectories? userData = null)
    {
        UserDataDirectories? ownUserData = userData is null ? new UserDataDirectories() : null;
        List<GameProcess> processes = [];
        List<(GameProcess Process, string Problem)> lifecycleProblems = [];
        try
        {
            bool running = Launch(launches, userData ?? ownUserData!, processes, lifecycleProblems)
                && LingerAndCheck(processes, lifecycleProblems);

            if (running && departure is not null)
            {
                Depart(departure, launches, processes, lifecycleProblems);
            }

            if (running && selfExit is not null)
            {
                GameProcess exiter = ProcessOf(selfExit.Exiter, launches, processes);
                selfExit.Cause();
                AddProblem(lifecycleProblems, exiter, exiter.WaitForSelfExit(SelfExitTimeout));
            }

            // Stopping before scanning is deliberate: the Serilog sink is asynchronous, so the last
            // lines only arrive as the process shuts down.
            foreach (GameProcess process in processes)
            {
                AddProblem(lifecycleProblems, process, process.Stop());
            }

            if (running)
            {
                CheckExitMilestones(launches, processes, lifecycleProblems);
            }

            Report(processes, lifecycleProblems);
        }
        finally
        {
            foreach (GameProcess process in processes)
            {
                process.Dispose();
            }
            ownUserData?.Dispose();
        }
    }

    /// <summary>
    /// Starts the processes one by one. False when one of them missed a milestone.
    /// </summary>
    private static bool Launch(
        IReadOnlyList<GameLaunch> launches,
        UserDataDirectories userData,
        List<GameProcess> processes,
        List<(GameProcess Process, string Problem)> problems)
    {
        foreach (GameLaunch launch in launches)
        {
            GameProcess process = GameProcess.Start(launch.Name, launch.Arguments, userData.Of(launch.Name));
            processes.Add(process);

            string? problem = ReachMilestones(process, launch.Milestones, StartupTimeout);
            if (problem is null) continue;

            problems.Add((process, problem));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Lets every process that is still meant to run live a little longer. False when one of them
    /// exited on its own meanwhile.
    /// </summary>
    private static bool LingerAndCheck(
        IEnumerable<GameProcess> processes, List<(GameProcess Process, string Problem)> problems)
    {
        Thread.Sleep(Linger);

        List<GameProcess> exited = processes.Where(process => process.HasExited).ToList();
        problems.AddRange(exited.Select(process => (process,
            $"exited on its own with code {process.ExitCode} while it should have kept running")));

        return exited.Count == 0;
    }

    private static void Depart(
        Departure departure,
        IReadOnlyList<GameLaunch> launches,
        IReadOnlyList<GameProcess> processes,
        List<(GameProcess Process, string Problem)> problems)
    {
        GameProcess leaver = ProcessOf(departure.Leaver, launches, processes);
        GameProcess witness = ProcessOf(departure.Witness, launches, processes);

        AddProblem(problems, leaver, leaver.Stop());

        string? problem = ReachMilestones(witness, [departure.Milestone], DepartureTimeout);
        if (problem is not null)
        {
            problems.Add((witness, $"after {leaver.Name} quit, {problem}"));
            return;
        }

        LingerAndCheck(processes.Where(process => process != leaver), problems);
    }

    /// <summary>
    /// Runs on stopped processes: the Serilog sink is asynchronous, so a line printed on the way out arrives
    /// only as the process shuts down.
    /// </summary>
    private static void CheckExitMilestones(
        IReadOnlyList<GameLaunch> launches,
        IReadOnlyList<GameProcess> processes,
        List<(GameProcess Process, string Problem)> problems)
    {
        for (int i = 0; i < launches.Count; i++)
        {
            foreach (string milestone in launches[i].ExitMilestones)
            {
                if (processes[i].HasPrinted(milestone)) continue;

                problems.Add((processes[i], $"did not print '{milestone}' by the time it was stopped"));
            }
        }
    }

    /// <summary>
    /// The process launched as <paramref name="name"/>. A departure or a self-exit naming no launch is a mistake
    /// in the scenario itself, not a game failure, so it throws instead of reporting.
    /// </summary>
    private static GameProcess ProcessOf(
        string name, IReadOnlyList<GameLaunch> launches, IReadOnlyList<GameProcess> processes)
    {
        for (int i = 0; i < launches.Count; i++)
        {
            if (launches[i].Name == name) return processes[i];
        }

        throw new ArgumentException($"The scenario names '{name}', but none of its processes is called that.");
    }

    /// <summary>
    /// Null when the process printed every milestone in time; otherwise what went wrong.
    /// </summary>
    private static string? ReachMilestones(GameProcess process, IReadOnlyList<string> milestones, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;

        foreach (string milestone in milestones)
        {
            if (process.WaitForLine(milestone, deadline)) continue;

            return process.HasExited
                ? $"exited with code {process.ExitCode} before printing '{milestone}'"
                : $"did not print '{milestone}' within {timeout.TotalSeconds:0} s";
        }

        return null;
    }

    private static void AddProblem(
        List<(GameProcess Process, string Problem)> problems, GameProcess process, string? problem)
    {
        if (problem is not null) problems.Add((process, problem));
    }

    /// <summary>
    /// Collects violations from every process into one list instead of failing on the first, the way
    /// the unit tests do — fixing them one round-trip at a time is the alternative.
    /// </summary>
    private static void Report(
        IReadOnlyList<GameProcess> processes, IReadOnlyList<(GameProcess Process, string Problem)> lifecycleProblems)
    {
        List<Problem> problems = [];

        foreach ((GameProcess process, string problem) in lifecycleProblems)
        {
            problems.Add(new Problem(
                process.Name,
                $"{problem}. Last lines of its output:",
                process.Output.TakeLast(TailLineCount).ToList()));
        }

        foreach (GameProcess process in processes)
        {
            problems.AddRange(process.ChildProblems.Select(problem => new Problem(process.Name, problem, [])));
            problems.AddRange(OutputScanner.Scan(process));
        }

        string? report = ProblemReport.Render(problems);
        if (report is not null) Assert.Fail(report);
    }
}
