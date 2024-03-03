using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Structs;

namespace NeoVector;

public partial class GroupSpawner : Node2D
{
    public double Radius { get; set; }
    public double Amount { get; set; }
    public BattleWorld World { get; set; }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Amount > 0)
        {
            var position = Rand.InsideCircle(new Circle(Position, Radius));
            Amount--;
            EventBus.Publish(new BattleWorldSpawnEnemyRequest(World, position));
        }
        else
        {
            QueueFree();
        }
    }
}