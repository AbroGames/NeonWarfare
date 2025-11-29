using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character;

public partial class Character : RigidBody2D
{
    
    [Child] public Sprite2D Sprite { get; private set; }
    [Child] public Area2D HitBox { get; private set; }

    public ICharacterController Controller { get; private set; }
    
    public CharacterStats Stats { get; private set; }
    public CharacterStatsClient StatsClient { get; private set; }
    public CharacterStatusEffects StatusEffects { get; private set; }
    public CharacterStatusEffectsClient StatusEffectsClient { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);
        
        var synchronizer = this.FindChild<CharacterSynchronizer>();
        Net.DoServerClient(
            () => Stats = new CharacterStats(this, synchronizer),
            () => StatsClient = new CharacterStatsClient(this, synchronizer));
        Net.DoServerClient(
            () => StatusEffects = new CharacterStatusEffects(this, synchronizer),
            () => StatusEffectsClient = new CharacterStatusEffectsClient(this, synchronizer));
    }

    public override void _PhysicsProcess(double delta)
    {
        Stats.OnPhysicsProcess(delta);
        StatsClient.OnPhysicsProcess(delta);
        StatusEffects.OnPhysicsProcess(delta);
        StatusEffectsClient.OnPhysicsProcess(delta);
    }
}
