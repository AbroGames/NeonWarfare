using System.Diagnostics;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// A process that does nothing but exist until it is killed, for a game process to watch as its parent.
/// Needs <c>sleep</c> on PATH, so Unix only.
/// </summary>
public sealed class StandInProcess : IDisposable
{
    private readonly Process _process = Process.Start(new ProcessStartInfo("sleep", "600") { UseShellExecute = false })
        ?? throw new InvalidOperationException("Failed to start `sleep` for a stand-in process.");

    public int Pid => _process.Id;

    /// <summary>
    /// Returns once the process is reaped: a zombie still shows up in the process list a game process reads.
    /// </summary>
    public void Kill()
    {
        _process.Kill();
        _process.WaitForExit();
    }

    public void Dispose()
    {
        if (!_process.HasExited) Kill();
        _process.Dispose();
    }
}
