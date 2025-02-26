using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemy : ServerCharacter
{
    [Export] [NotNull] public RayCast2D RayCast { get; private set; }
    private const double TargetChangeTimeoutSec = 3; 

    public ServerCharacter Target { get; set; }
    private AutoCooldown _changeTargetCooldown = new(TargetChangeTimeoutSec);

    public override void _Ready()
    {
        base._Ready();
        
        UpdateTarget();
        _changeTargetCooldown.ActionWhenReady += UpdateTarget;
        _changeTargetCooldown.Update(Rand.Range(0.0, TargetChangeTimeoutSec)); //Для того чтобы одновременно заспауненные враги не переключались все синхронно.
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        _changeTargetCooldown.Update(delta);
    }

    private void UpdateTarget()
    {
        ServerPlayer closestPlayer = null;
        double closestDistance = double.MaxValue;
        
        foreach (ServerPlayer player in ServerRoot.Instance.Game.World.Players)
        {
            double distance = this.DistanceTo(player);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        Target = closestPlayer;
    }
}
