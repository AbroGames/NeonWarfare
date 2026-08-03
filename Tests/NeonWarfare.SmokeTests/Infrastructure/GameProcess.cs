using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One launched instance of the game: starts it, captures everything it prints, and stops it.
///
/// Output is captured from the process rather than read from user://logs/godot.log, because a
/// scenario runs several instances of the same Godot project at once — they all write to that one
/// file and rotate it out from under each other on startup.
/// </summary>
public sealed class GameProcess : IDisposable
{
    /// <summary>
    /// How long a SIGTERM is given to bring the process down before it is killed outright.
    /// </summary>
    private const int GracefulShutdownTimeoutMs = 5000;

    private const int Sigterm = 15;

    private readonly Process _process;
    private readonly List<string> _output = [];
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
    /// Everything the process has printed so far, stdout and stderr merged in arrival order.
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
    /// Launches the engine against the repository. <paramref name="arguments"/> are the game flags —
    /// --path and --headless are added here, since every scenario needs them.
    /// </summary>
    public static GameProcess Start(string name, params string[] arguments)
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
    /// Asks the process to quit and waits for it.
    ///
    /// SIGTERM first, on purpose. The Serilog sink is wrapped in WriteTo.Async and nothing calls
    /// Log.CloseAndFlush(), so a hard kill drops whatever is still queued — including the error that
    /// would have explained the failure. A graceful exit also runs the autosave path (Docs/Shutdown.md).
    /// </summary>
    public void Stop()
    {
        if (_process.HasExited) return;

        if (TryRequestTermination() && _process.WaitForExit(GracefulShutdownTimeoutMs)) return;

        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(GracefulShutdownTimeoutMs);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the check and the kill. Nothing to do.
        }
    }

    public void Dispose()
    {
        try
        {
            Stop();
        }
        finally
        {
            _process.Dispose();
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

        lock (_outputLock)
        {
            _output.Add(line);
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int NativeKill(int pid, int signal);
}
