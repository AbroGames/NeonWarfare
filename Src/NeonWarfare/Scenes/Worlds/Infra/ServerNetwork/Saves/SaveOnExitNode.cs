using System;
using Godot;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;

/// <summary>
/// Saves a server World when it leaves the tree: on quit and on the way back to the menu.
/// </summary>
public partial class SaveOnExitNode : Node
{
    private Action _saveOnExit;

    public SaveOnExitNode InitPreReady(Action saveOnExit)
    {
        _saveOnExit = saveOnExit;
        return this;
    }

    // Between ticks: Quit() only sets a flag, the tree is torn down after the physics step, so the baselines are the
    // end of the last one. The World's services live until its predelete, which comes after this
    public override void _ExitTree() => _saveOnExit();
}
