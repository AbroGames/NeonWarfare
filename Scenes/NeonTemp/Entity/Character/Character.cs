using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character;

public partial class Character : RigidBody2D
{
    
    [Child] public Sprite2D Sprite { get; private set; }
    [Child] public Area2D HitBox { get; private set; }

    public CharacterController Controller { get; private set; }
    
    public CharacterStats Stats { get; private set; }
    public CharacterStatsClient StatsClient { get; private set; }
    public CharacterStatusEffects StatusEffects { get; private set; }
    public CharacterStatusEffectsClient StatusEffectsClient { get; private set; }
    
    [Child] private CharacterSynchronizer _synchronizer;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        Controller = new(this);
        
        Net.DoServer(() => Stats = new CharacterStats(this, _synchronizer));
        Net.DoClient(() => StatsClient = new CharacterStatsClient(this, _synchronizer));
        Net.DoServer(() => StatusEffects = new CharacterStatusEffects(this, _synchronizer));
        Net.DoClient(() => StatusEffectsClient = new CharacterStatusEffectsClient(this, _synchronizer));
    }

    public override void _PhysicsProcess(double delta)
    {
        Stats.OnPhysicsProcess(delta);
        StatsClient.OnPhysicsProcess(delta);
        StatusEffects.OnPhysicsProcess(delta);
        StatusEffectsClient.OnPhysicsProcess(delta);
    }
}
