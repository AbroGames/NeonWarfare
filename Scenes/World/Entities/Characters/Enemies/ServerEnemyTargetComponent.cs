using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemyTargetComponent : Node
{
    private const double TargetChangeTimeoutSec = 3; 

    public ServerCharacter Target { get; set; }
    
    private ServerEnemy _parent;
    private AutoCooldown _changeTargetCooldown = new(TargetChangeTimeoutSec);
    
    public override void _Ready()
    {
        _parent = GetParent<ServerEnemy>();
        
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
            double distance = _parent.DistanceTo(player);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        Target = closestPlayer;
    }
}
