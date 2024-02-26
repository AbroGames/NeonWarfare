using Godot;

namespace AbroDraft.World;

public class Punch(Vector2 dir, double strength, double movementSpeed = 3000) : IShiftProvider
{
    public Vector2 Direction { get; private set; } = dir.Normalized();
    public double Strength { get; private set; } = strength;
    public double InitialStrength { get; private set; } = strength;

    public Vector2 Shift => Direction * Strength;
    public bool IsAlive => Strength > Mathf.Epsilon;
    public void Update(double delta)
    {
        Strength = Mathf.Max(0, Strength - movementSpeed * delta);
    }
}