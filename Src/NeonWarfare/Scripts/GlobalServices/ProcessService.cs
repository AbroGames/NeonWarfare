using System;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using NeonWarfare.Scripts.Content.LoadingScreen;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices;

public class ProcessService
{

    // Godot's own flag, not a game argument: the engine consumes it before the game sees the command line
    private const string GodotHeadlessFlag = "--headless";

    private const double ExitPollSeconds = 0.1;
    // Far longer than an autosave takes: a server still running after it has hung
    private const ulong ExitTimeoutMsec = 10_000;

    private const string ServerExitedLog = "Dedicated server process {pid} has exited.";
    private const string ServerHungLog = "Dedicated server process {pid} has not exited in {timeoutMsec} ms. Kill it.";

    private readonly ILogger _log = LogFactory.GetForStatic<ProcessService>();

    private SceneTree _sceneTree;
    private int? _dedicatedServerPid;
    private Action _onServerExited;

    public void Init(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;
    }

    public int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }

    public void StartNewDedicatedServerApplication(string saveFileName, int port, string adminUid, bool showWindow)
    {
        CommonArgs commonArgs = new CommonArgs(
            false); // Dedicated server never uses Godot console
        DedicatedServerArgs dedicatedServerArgs = new DedicatedServerArgs(
            commonArgs,
            port,
            saveFileName,
            adminUid,
            OS.GetProcessId());

        string[] arguments = dedicatedServerArgs.GetArrayToStartDedicatedServer();
        _dedicatedServerPid = StartNewApplication(showWindow ? arguments : [..arguments, GodotHeadlessFlag]);
    }

    /// <summary>
    /// The child server stops by itself once its admin leaves, and autosaves on the way out. This only waits for it
    /// behind the loading screen, so a new server never starts beside the old one still saving; a server that hangs
    /// is killed after the timeout. Without a running child server <paramref name="onExited"/> runs right away.
    /// A call during the wait replaces <paramref name="onExited"/>: quitting while leaving for the menu must quit.
    /// </summary>
    public void WaitForDedicatedServerExit(Action onExited)
    {
        if (_onServerExited != null)
        {
            _onServerExited = onExited;
            return;
        }

        if (_dedicatedServerPid is not { } pid || !OS.IsProcessRunning(pid))
        {
            _dedicatedServerPid = null;
            onExited();
            return;
        }

        _onServerExited = onExited;
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.StoppingServer);
        ulong deadlineMsec = Time.GetTicksMsec() + ExitTimeoutMsec;
        Poll();

        void Poll()
        {
            if (OS.IsProcessRunning(pid))
            {
                if (Time.GetTicksMsec() < deadlineMsec)
                {
                    _sceneTree.CreateTimer(ExitPollSeconds).Timeout += Poll;
                    return;
                }
                _log.Warning(ServerHungLog, pid, ExitTimeoutMsec);
                OS.Kill(pid);
            }
            else
            {
                _log.Information(ServerExitedLog, pid);
            }

            _dedicatedServerPid = null;
            Services.LoadingScreen.Clear();
            Action then = _onServerExited;
            _onServerExited = null;
            then();
        }
    }
}
