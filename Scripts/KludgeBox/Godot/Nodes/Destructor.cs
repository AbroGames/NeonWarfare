using Godot;
using NeonWarfare.Scripts.KludgeBox.Scheduling;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

[GlobalClass]
public partial class Destructor : Node
{
    public bool IsPaused { get; set; } = false;

    public double TimeLeft
    {
        get => _cooldown.TimeLeft;
        set
        {
            _cooldown.Duration = value;
            _cooldown.Restart();
        }
    }

    private Cooldown _cooldown = new Cooldown();

    public Destructor()
    {
        _cooldown.Ready += Destruct;
        TimeLeft = 0;
    }

    public Destructor(double time)
    {
        TimeLeft = time;
        _cooldown.Ready += Destruct;
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
