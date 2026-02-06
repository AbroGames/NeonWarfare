using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Remote;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character;

public partial class Character : RigidBody2D
{
    
    [Child] public Sprite2D Sprite { get; private set; }
    [Child] public Area2D HitBox { get; private set; }
    
    public CharacterStats Stats { get; private set; }
    public CharacterStatsClient StatsClient { get; private set; }
    public CharacterStatusEffects StatusEffects { get; private set; }
    public CharacterStatusEffectsClient StatusEffectsClient { get; private set; }

    public CharacterController Controller { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);
        //TODO Добавлять самого себя в какой-нибудь кеш? Это анти-паттерн, но иначе на клиенте сложно отследить спавн врага и добавить его
        //TODO Инжектить сервис из World через Di и добавлять через него? Все равно, скорее всего, из Character потребуется доступ к World
        //TODO С другой стороны, всеми силами хотелось бы этого избежать, т.к. это нарушение принципа: вызовы вниз, сигналы вверх
        //TODO Но привязаться к сигналам спаунящегося юнита нельзя, если не кастомить спаунер. Или можно? Просто в tree отслеживать ивент добавления или типа того
        
        var synchronizer = this.FindChild<CharacterSynchronizer>();
        Net.DoServerClient(
            () => Stats = new CharacterStats(this, synchronizer),
            () => StatsClient = new CharacterStatsClient(this, synchronizer));
        Net.DoServerClient(
            () => StatusEffects = new CharacterStatusEffects(this, synchronizer),
            () => StatusEffectsClient = new CharacterStatusEffectsClient(this, synchronizer));
        Net.DoServerNotServer(
            () => Controller = new CharacterController(this, synchronizer),
            () => Controller = new CharacterController(this, synchronizer, new RemoteController()));

        synchronizer.InitPostReady(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        Stats?.OnPhysicsProcess(delta);
        StatsClient?.OnPhysicsProcess(delta);
        StatusEffects?.OnPhysicsProcess(delta);
        StatusEffectsClient?.OnPhysicsProcess(delta);
        
        Controller.OnPhysicsProcess(delta);
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        Controller.OnIntegrateForces(state);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        Controller.OnUnhandledInput(@event, GetViewport().SetInputAsHandled);
    }
}
