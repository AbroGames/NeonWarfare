using Godot;
using KludgeBox;

namespace KludgeBox.Events.Global.World;

public class ManualShake : IShiftProvider
{
    /// <inheritdoc />
    public Vector2 Shift => IsAlive ? Rand.InsideUnitCircle * Strength : Vec();

    public double Strength { get; set; } = 0;

    /// <inheritdoc />
    public bool IsAlive { get; set; } = true;

    /// <inheritdoc />
    public void Update(double delta)
    {
        // do nothing
    }
}