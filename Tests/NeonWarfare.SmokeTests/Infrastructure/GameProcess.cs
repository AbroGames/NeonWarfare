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

    private readonly Process _process;
    private readonly string _userDataPath;
    private readonly List<string> _output = [];
    private readonly Lock _outputLock = new();


    private GameProcess(string name, Process process, string userDataPath)
    {
        Name = name;
        _process = process;
        _userDataPath = userDataPath;
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
    /// Launches the engine against the repository. <paramref name="arguments"/> are the game flags —
    /// --path and --headless are added here, since every scenario needs them.
    /// </summary>
    public static GameProcess Start(string name, IEnumerable<string> arguments)
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

        string userDataPath = IsolateUserData(startInfo, name);

        Process process = new() { StartInfo = startInfo };
        GameProcess gameProcess = new(name, process, userDataPath);

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
            DeleteUserData();
        }
    }

    /// <summary>
    /// Points user:// of the launched process at a fresh temporary directory, so the developer's own
    /// settings and saves neither affect the result nor get polluted by it, and processes of one scenario
    /// do not share settings, saves or godot.log. Godot derives user:// from XDG_DATA_HOME on Linux
    /// (checked) and from APPDATA on Windows (not verified). On macOS it hangs off HOME, which is too
    /// broad to override, so there the real user:// is still used.
    /// </summary>
    private static string IsolateUserData(ProcessStartInfo startInfo, string name)
    {
        string path = Path.Combine(Path.GetTempPath(), "NeonWarfare.SmokeTests", $"{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        if (OperatingSystem.IsLinux())
        {
            startInfo.Environment["XDG_DATA_HOME"] = path;
        }
        else if (OperatingSystem.IsWindows())
        {
            startInfo.Environment["APPDATA"] = path;
        }

        return path;
    }

    private void DeleteUserData()
    {
        try
        {
            Directory.Delete(_userDataPath, recursive: true);
        }
        catch (IOException)
        {
            // Left behind in the temp directory — harmless, and not worth failing a test over.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above.
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
    /// overload waits for the redirected streams to reach their end.
    /// </summary>
    private void DrainOutput() => _process.WaitForExit();

    private bool HasPrinted(string fragment)
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
        lock (_outputLock)
        {
            _output.Add(plain);
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int NativeKill(int pid, int signal);
}
