using Godot;

namespace NeoVector;

public interface IShiftProvider
{
    public abstract Vector2 Shift { get; }
    public abstract bool IsAlive { get; }
    public abstract void Update(double delta);
}