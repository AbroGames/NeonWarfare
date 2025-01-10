using Godot;
using KludgeBox;

namespace NeonWarfare;

public class ManualShake : IShiftProvider
{
    /// <inheritdoc />
    public Vector2 Shift => IsAlive ? Rand.InsideUnitCircle * Strength : Vec();

    public float Strength { get; set; } = 0;

    /// <inheritdoc />
    public bool IsAlive { get; set; } = true;

    /// <inheritdoc />
    public void Update(double delta)
    {
        // do nothing
    }
}