using Godot;

namespace NeonWarfare.Scripts.GlobalServices;

/// <summary>
/// A quit requested from outside the game — a termination signal or closing the window — goes through
/// <see cref="MainSceneService.Shutdown"/>, the same way as the game's own Quit: it autosaves and waits for a child
/// server.
/// </summary>
public class QuitRequestsService
{
    private readonly TerminationSignals _terminationSignals = new();

    /// <summary>
    /// Goes through MainScene, so must be called only after <see cref="MainSceneService.Init"/>.
    /// </summary>
    public void Init(SceneTree sceneTree)
    {
        _terminationSignals.Init();
        // Otherwise the engine quits on its own as soon as the window is closed, with nothing to wait for the child
        sceneTree.AutoAcceptQuit = false;
        sceneTree.Root.CloseRequested += Services.MainScene.Shutdown;
    }

    private class TerminationSignals : KludgeBox.Godot.Services.TerminationSignalsService
    {
        // Runs on a runtime thread, not the main one — MainScene.Shutdown() only queues a deferred call
        protected override void Shutdown() => Services.MainScene.Shutdown();
    }
}
