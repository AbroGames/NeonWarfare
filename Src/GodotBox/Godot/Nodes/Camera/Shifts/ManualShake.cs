using Godot;

namespace GodotBox.Godot.Nodes.Camera.Shifts;

public class ManualShake : IShiftProvider
{
    public Vector2 Shift => IsAlive ? Services.Rand.InsideUnitCircle * Strength : Vec2();
    public float Strength { get; set; } = 0;
    public bool IsAlive { get; set; } = true;

    public void Update(double delta)
    {
        // do nothing
    }
}
