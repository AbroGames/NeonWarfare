using Godot;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

[GlobalClass]
public partial class Destructor : Node
{
    public bool IsPaused { get; set; } = false;

    public double TimeLeft
    {
        get => _cooldown.TimeLeft;
        set => _cooldown = new ManualCooldown(value, false, true, Destruct);
    }

    private ManualCooldown _cooldown;

    public Destructor(double time)
    {
        TimeLeft = time;
        _cooldown = new ManualCooldown(time, false, true, Destruct);
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _cooldown.Update(0);
    }

    public override void _Process(double delta)
    {
        _cooldown.Update(delta);
    }

    private void Destruct()
    {
        var parent = GetParent();
        if (IsInstanceValid(parent))
            parent.QueueFree();
        QueueFree();
    }
}
