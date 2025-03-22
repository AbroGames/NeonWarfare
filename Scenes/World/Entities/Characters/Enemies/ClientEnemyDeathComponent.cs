using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ClientEnemyDeathComponent : ClientEnemyComponentBase
{
    private const float BaseScaleFactor = 0.1f;

    public override void _ExitTree()
    {
        var deathEffect = Fx.CreateDeathFx();
        var debrisEffect = Fx.CreateDebrisFx();

        var color = Parent.GetColor();
        deathEffect.Modulate = color;
        debrisEffect.Modulate = color;
        
        deathEffect.Position = Parent.Position;
        debrisEffect.Position = Parent.Position;
        
        var scaleFactorRatio = Parent.GetScaleFactor() / BaseScaleFactor;
        deathEffect.Scale *= scaleFactorRatio;
        debrisEffect.Scale *= scaleFactorRatio;

        var world = Parent.GetParent();
        world.TryAddChildDeferred(deathEffect, () => world.MoveChild(deathEffect, 1));
        world.TryAddChildDeferred(debrisEffect, () => world.MoveChild(debrisEffect, 1));
    }
}