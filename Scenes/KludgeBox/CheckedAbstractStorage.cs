using AbstractStorage = KludgeBox.Godot.Nodes.AbstractStorage;

namespace NeonWarfare.Scenes.KludgeBox;

public abstract partial class CheckedAbstractStorage : AbstractStorage
{
    public override void _PreReady()
    {
        Di.Process(this);
    }
}