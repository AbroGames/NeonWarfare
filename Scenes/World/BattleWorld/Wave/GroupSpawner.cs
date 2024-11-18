using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Structs;

namespace NeonWarfare;

public partial class GroupSpawner : Node2D
{
    public float Radius { get; set; }
    public float Amount { get; set; }
    public ServerBattleWorld World { get; set; }

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