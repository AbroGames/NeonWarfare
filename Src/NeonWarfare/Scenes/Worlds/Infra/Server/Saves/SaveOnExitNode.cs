using System;
using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>Saves a server World when it leaves the tree, on quit and on the way back to the menu.</summary>
public partial class SaveOnExitNode : Node
{
    private Action _saveOnExit;

    public SaveOnExitNode InitPreReady(Action saveOnExit)
    {
        _saveOnExit = saveOnExit;
        return this;
    }

    /// <summary>
    /// This always runs between ticks, so the baselines hold the state at the end of the last tick.
    /// <c>Quit()</c> only sets a flag, and the tree is torn down after the physics step.
    /// The World's services are still alive here, because they live until the World's predelete.
    /// </summary>
    public override void _ExitTree() => _saveOnExit();
}
