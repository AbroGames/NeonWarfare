using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scripts.Utils.Camera.Shifts;

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
