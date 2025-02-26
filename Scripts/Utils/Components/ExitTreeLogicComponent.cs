using System;
using Godot;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class ExitTreeLogicComponent : Node
{
    private event Action<Node> _actionWhenExitTree;

    public override void _ExitTree()
    {
        _actionWhenExitTree.Invoke(GetParent());
    }
    
    public void AddActionWhenExitTree(Action action)
    {
        _actionWhenExitTree += (node) => action.Invoke();
    }

    public void AddActionWhenExitTree(Action<Node> action)
    {
        _actionWhenExitTree += action;
    }
}