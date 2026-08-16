using GodotBox.Godot.Nodes;
using KludgeBox.DI;

namespace NeonWarfare.Scenes.Misc;

/// <summary>
/// Storage with auto calling <c>Di.Process(this)</c>.<br/>
/// <br/>
/// <b>You must add [NotNull] to every field that requires validation.</b>
/// </summary>
public abstract partial class GameCheckedAbstractStorage : CheckedAbstractStorage
{
    public override DependencyInjector GetDi()
    {
        return Di;
    }
}