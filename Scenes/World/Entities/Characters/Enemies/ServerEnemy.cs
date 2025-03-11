using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemy : ServerCharacter
{
    [Export] [NotNull] public RayCast2D RayCast { get; private set; }


    public void InitComponents()
    {
        AddChild(new ServerEnemyMovementComponent());

        ServerEnemyTargetComponent serverEnemyTargetComponent = new ServerEnemyTargetComponent();
        AddChild(serverEnemyTargetComponent);
        
        RotateComponent rotateComponent = new RotateComponent();
        rotateComponent.GetTargetFunc = () =>
        {
            if (serverEnemyTargetComponent.Target == null || !serverEnemyTargetComponent.Target.IsValid()) return null;
            return serverEnemyTargetComponent.Target.GlobalPosition;
        };
        rotateComponent.GetRotationSpeedFunc = () => RotationSpeed;
        AddChild(rotateComponent);
    }

    public void InitOnSpawn(Vector2 position, float rotation)
    {
        Position = position;
        Rotation = rotation;
    }

}
