using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One launched instance of the game: starts it, captures everything it prints, and stops it.
///
/// Output is captured from the process rather than read from user://logs/godot.log: the stream is
/// there as the lines arrive, which is what waiting for a milestone needs.
/// </summary>
public sealed partial class GameProcess : IDisposable
{
    /// <summary>
    /// How long a SIGTERM is given to bring the process down before it is killed outright.
    /// </summary>
    private static readonly TimeSpan GracefulShutdownTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How often <see cref="WaitForLine"/> looks at the output again.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private const int Sigterm = 15;

    /// <summary>
    /// How much later than its start line a child may seem to have started and still be that child. The start time
    /// is derived from the boot time, so it drifts a little, while a pid reused by another process starts no earlier
    /// than the child's exit, seconds after the line.
    /// </summary>
    private static readonly TimeSpan ChildStartTolerance = TimeSpan.FromSeconds(1);

    private readonly Process _process;
    private readonly List<string> _output = [];
    private readonly List<ChildServer> _children = [];
    private readonly List<string> _childProblems = [];
    private readonly Lock _outputLock = new();

    private GameProcess(string name, Process process)
    {
        Name = name;
        _process = process;
    }

    /// <summary>
    /// Human-readable name used to attribute output lines in a failure report.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Everything the process has printed so far, stdout and stderr merged in arrival order, with ANSI
    /// escapes already stripped.
    /// </summary>
    public IReadOnlyList<string> Output
    {
        get
        {
            lock (_outputLock)
            {
                return _output.ToArray();
            }
        }
    }

    /// <summary>
    /// The child processes found still running once this one had exited. A client waits for its child server to
    /// exit, so every such child is a bug of the game; it is killed by then.
    /// </summary>
    public IReadOnlyList<string> ChildProblems
    {
        get
        {
            lock (_outputLock)
            {
                return _childProblems.ToArray();
            }
        }
    }

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.ExitCode;

    /// <summary>
    /// GD.PrintRich turns BBCode into ANSI escapes even when stdout is redirected (checked on 4.7.1), so
    /// a Serilog line starts with "|\x1B[38;2;...m" rather than with its timestamp. Stripped once here,
    /// so neither the scanner nor a milestone has to know about it.
    /// </summary>
    [GeneratedRegex(@"\x1B\[[0-9;]*m")]
    private static partial Regex AnsiEscapeRegex();

    /// <summary>
    /// ProcessService: the client has started a child dedicated server. The child inherits the client's stdout, so
    /// its lines arrive in the same stream, and the stream ends only once the child has exited too.
    /// </summary>
    [GeneratedRegex(@"Started dedicated server process (?<pid>\d+)")]
    private static partial Regex ChildStartedRegex();

    /// <summary>
    /// Launches the engine against the repository. <paramref name="arguments"/> are the game flags —
    /// --path and --headless are added here, since every scenario needs them. <paramref name="userDataPath"/>
    /// is the directory Godot puts its user:// under.
    /// </summary>
    public static GameProcess Start(string name, IEnumerable<string> arguments, string userDataPath)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = GodotExecutable.Path,
            WorkingDirectory = RepositoryRoot.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // The game reads OS.GetCmdlineArgs() and matches flags by exact string, taking a value from
        // the next argv element. So no "--" separator (that would move them into the user args) and
        // no "--flag=value" form.
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(RepositoryRoot.Path);
        startInfo.ArgumentList.Add("--headless");
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        IsolateUserData(startInfo, userDataPath);

        Process process = new() { StartInfo = startInfo };
        GameProcess gameProcess = new(name, process);

        process.OutputDataReceived += (_, args) => gameProcess.Collect(args.Data);
        process.ErrorDataReceived += (_, args) => gameProcess.Collect(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return gameProcess;
    }

    /// <summary>
    /// Waits until some output line contains <paramref name="fragment"/>. False when the deadline
    /// passes or the process exits without printing it — <see cref="HasExited"/> tells the two apart.
    /// </summary>
    public bool WaitForLine(string fragment, DateTime deadline)
    {
        while (true)
        {
            if (HasPrinted(fragment)) return true;

            if (_process.HasExited)
            {
                DrainOutput();
                return HasPrinted(fragment);
            }

            if (DateTime.UtcNow >= deadline) return false;

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Asks the process to quit and waits for it, output included. Null when it quit the way the game
    /// is meant to — on its own within the timeout and with exit code 0; otherwise what went wrong.
    ///
    /// SIGTERM first, on purpose. The Serilog sink is wrapped in WriteTo.Async and nothing calls
    /// Log.CloseAndFlush(), so a hard kill drops whatever is still queued — including the error that
    /// would have explained the failure. A graceful exit also runs the autosave path (Docs/Shutdown.md),
    /// and that is exactly what a hang or a crash on the way out breaks.
    ///
    /// A process that had already exited before the call is not judged here: dying early is a problem of
    /// its own, and the caller reports it with more context. Neither is one on a platform without
    /// SIGTERM (Windows): there is nothing to ask it politely with, and a kill says nothing about the game.
    /// </summary>
    public string? Stop()
    {
        if (_process.HasExited)
        {
            DrainOutput();
            return null;
        }

        if (!TryRequestTermination())
        {
            Kill();
            return null;
        }

        if (!_process.WaitForExit(GracefulShutdownTimeout))
        {
            Kill();
            return $"did not exit within {GracefulShutdownTimeout.TotalSeconds:0} s of SIGTERM and was killed";
        }

        DrainOutput();
        return _process.ExitCode == 0 ? null : $"exited with code {_process.ExitCode} after SIGTERM";
    }

    /// <summary>
    /// Waits for the process to quit without being asked. Null when it did, within <paramref name="timeout"/> and
    /// with exit code 0; otherwise what went wrong. A process still running is left for <see cref="Stop"/>.
    /// </summary>
    public string? WaitForSelfExit(TimeSpan timeout)
    {
        if (!_process.WaitForExit(timeout))
        {
            return $"did not exit on its own within {timeout.TotalSeconds:0} s";
        }

        DrainOutput();
        return _process.ExitCode == 0 ? null : $"exited on its own with code {_process.ExitCode}";
    }

    public void Dispose()
    {
        try
        {
            // A safety net for a run that threw halfway: a scenario stops and judges its processes
            // itself, so by now there is normally nothing left to stop and nothing to report.
            Stop();
        }
        finally
        {
            _process.Dispose();
        }
    }

    /// <summary>
    /// Godot derives user:// from XDG_DATA_HOME on Linux (checked) and from APPDATA on Windows (not
    /// verified). On macOS it hangs off HOME, which is too broad to override, so there the real user:// is
    /// still used.
    /// </summary>
    private static void IsolateUserData(ProcessStartInfo startInfo, string path)
    {
        if (OperatingSystem.IsLinux())
        {
            startInfo.Environment["XDG_DATA_HOME"] = path;
        }
        else if (OperatingSystem.IsWindows())
        {
            startInfo.Environment["APPDATA"] = path;
        }
    }

    private void Kill()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the check and the kill. Nothing to do.
        }

        DrainOutput();
    }

    /// <summary>
    /// WaitForExit(int) returning true does not mean the asynchronous output handlers are done — the
    /// last lines, the ones that explain a failure, can still be in flight. Only the parameterless
    /// overload waits for the redirected streams to reach their end, and a child still running keeps them open,
    /// so the children are killed first.
    /// </summary>
    private void DrainOutput()
    {
        KillLeftoverChildren();
        _process.WaitForExit();
    }

    private void KillLeftoverChildren()
    {
        ChildServer[] children;
        lock (_outputLock)
        {
            children = _children.ToArray();
            _children.Clear();
        }

        foreach (ChildServer child in children)
        {
            if (!TryKillRunning(child)) continue;

            lock (_outputLock)
            {
                _childProblems.Add(
                    $"child server {child.Pid} was still running after its client exited and was killed");
            }
        }
    }

    /// <summary>
    /// False when <paramref name="child"/> is no longer running. The child is not ours, so it is looked up by pid:
    /// it was reparented once its client exited. A process under that pid started after the child's start line is
    /// another one that reused the pid, and is left alone.
    /// </summary>
    private static bool TryKillRunning(ChildServer child)
    {
        try
        {
            using Process process = Process.GetProcessById(child.Pid);
            if (process.HasExited || process.StartTime > child.SeenAt + ChildStartTolerance) return false;

            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Looks only at the lines received so far: the whole output only once <see cref="Stop"/> has returned.
    /// </summary>
    public bool HasPrinted(string fragment)
    {
        lock (_outputLock)
        {
            return _output.Any(line => line.Contains(fragment, StringComparison.Ordinal));
        }
    }

    private bool TryRequestTermination()
    {
        // Process.Kill() is SIGKILL on Unix, so the polite signal has to go through libc.
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return false;

        try
        {
            return NativeKill(_process.Id, Sigterm) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private void Collect(string? line)
    {
        // A null line is the end-of-stream marker, not output.
        if (line is null) return;

        string plain = AnsiEscapeRegex().Replace(line, string.Empty);
        Match childStarted = ChildStartedRegex().Match(plain);
        lock (_outputLock)
        {
            _output.Add(plain);
            if (childStarted.Success)
            {
                _children.Add(new ChildServer(int.Parse(childStarted.Groups["pid"].Value), DateTime.Now));
            }
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int NativeKill(int pid, int signal);

    /// <summary>
    /// <paramref name="SeenAt"/> is local time, as <see cref="Process.StartTime"/> is.
    /// </summary>
    private readonly record struct ChildServer(int Pid, DateTime SeenAt);
}
