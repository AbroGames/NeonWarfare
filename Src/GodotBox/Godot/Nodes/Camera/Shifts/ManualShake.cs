using Godot;
using static KludgeBox.Godot.Extensions.VectorExtensions;

namespace GodotBox.Godot.Nodes.Camera.Shifts;

public class ManualShake : IShiftProvider
{
    public Vector2 Shift => IsAlive ? GodotBoxServices.Rand.InsideUnitCircle * Strength : Vec2();
    public float Strength { get; set; } = 0;
    public bool IsAlive { get; set; } = true;

    public void Update(double delta)
    {
        // do nothing
    }
}
