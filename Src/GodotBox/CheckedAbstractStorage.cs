using KludgeBox.DI;

namespace GodotBox;

// TODO: an we get rid of this class?
/// <summary>
/// Storage with auto calling <c>Di.Process(this)</c>.<br/>
/// <br/>
/// <b>You must add [NotNull] to every field that requires validation.</b>
/// </summary>
public abstract partial class CheckedAbstractStorage : Godot.Nodes.CheckedAbstractStorage
{
    public override DependencyInjector GetDi()
    {
        return Di;
    }
}