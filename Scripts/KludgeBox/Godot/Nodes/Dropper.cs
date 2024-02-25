using Godot;

namespace KludgeBox.Godot;

/// <summary>
/// Reparents node to its parents parent.
/// </summary>
[GlobalClass]
public partial class Dropper : Node
{
    [Export] public bool KeepParentModulate = true;
    /// <inheritdoc />
    public override void _Ready()
    {
        Callable.From(Drop).CallDeferred();
    }

    public void Drop()
    {
        var target = GetParent();
        var parent = target.GetParent();
        var newParent = parent.GetParent();
        
        target.Reparent(newParent, true);
        target.Owner = newParent;
        
        if (KeepParentModulate 
            && target is Node2D target2D 
            && parent is Node2D parent2D)
        {
            target2D.Modulate = parent2D.Modulate;
        }
        
        QueueFree();
    }
}