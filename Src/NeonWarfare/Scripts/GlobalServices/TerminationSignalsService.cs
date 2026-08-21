using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices;

public class TerminationSignalsService
{

    private static readonly PosixSignal[] TerminationSignals = [PosixSignal.SIGTERM, PosixSignal.SIGINT];

    [Logger] private ILogger _log;

    // Kept for the whole process lifetime: disposing a registration removes its handler.
    private readonly List<PosixSignalRegistration> _registrations = [];
    private int _terminationRequested;

    public TerminationSignalsService()
    {
        Di.Process(this);
    }

    /// <summary>
    /// Turns SIGTERM and SIGINT (Ctrl+C, <c>kill</c>, <c>systemctl stop</c>, <c>docker stop</c>) into
    /// <see cref="MainSceneService.Shutdown"/>. Godot installs no handler of its own, so without this the
    /// signal kills the process on the spot: no <c>NotificationExitTree</c>, no autosave, no network shutdown.
    /// Must be called after <see cref="MainSceneService.Init"/>, since the handler goes through it.
    /// </summary>
    public void Init()
    {
        foreach (PosixSignal signal in TerminationSignals)
        {
            _registrations.Add(PosixSignalRegistration.Create(signal, OnTerminationSignal));
        }
    }

    // Runs on a runtime thread, not the main one — MainScene.Shutdown() only queues a deferred call.
    private void OnTerminationSignal(PosixSignalContext context)
    {
        // A repeated signal is not cancelled and kills the process: the way out when a shutdown hangs.
        if (Interlocked.Exchange(ref _terminationRequested, 1) == 1) return;

        context.Cancel = true;
        _log.Information("Received {signal}, shutting down", context.Signal);
        Services.MainScene.Shutdown();
    }
}
