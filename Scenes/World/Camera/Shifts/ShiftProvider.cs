using Godot;

namespace NeoVector.World;

public interface IShiftProvider
{
    public abstract Vector2 Shift { get; }
    public abstract bool IsAlive { get; }
    public abstract void Update(double delta);
}