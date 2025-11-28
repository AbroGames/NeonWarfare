using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character;

public partial class Character : RigidBody2D
{
    
    [Child] public Sprite2D Sprite { get; private set; }
    [Child] public Area2D HitBox { get; private set; }

    public CharacterController Controller { get; private set; }
    public CharacterStatsNode Stats { get; private set; }
    public CharacterStatusEffects StatusEffects { get; private set; }
    public CharacterClientStatusEffects ClientStatusEffects { get; private set; }
    
    public Vector2 Vec;
    public PlayerController PlayerController;
    
    public float MaxSpeed = 200.0f;
    public float Acceleration = 10.0f;
    //public float Friction = 150.0f;
    
    private float ControlThreshold => MaxSpeed * 1.1f;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        Controller = new(this);
        Net.DoServer(() => StatusEffects = new CharacterStatusEffects(this));
        Net.DoClient(() => ClientStatusEffects = new CharacterClientStatusEffects(this));
        
        //ApplyCentralForce(Vec);
        //ApplyCentralImpulse(Vec);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionReleased(Keys.AttackPrimary))
        {
            ExplodeFromMouse();
            
            if (PlayerController != null) StatusEffects.AddStatusEffect(SimpleTempStatusEffect.Create("test-1", 2,
                StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MaxHp, 100)));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        StatusEffects.OnPhysicsProcess(delta);
        ClientStatusEffects.OnPhysicsProcess(delta);

        //MoveAndCollide(GetMovementInSecondFromInput() * (float) delta);
        //Velocity = GetMovementInSecondFromInput(); MoveAndSlide();

        //MoveAndCollide(Velocity * (float) delta);
        //MoveAndSlide();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        float delta = state.Step;
        Vector2 input = PlayerController?.GetMovementInput() ?? Vec;
        Vector2 targetVelocity = input * MaxSpeed;
        Vector2 currentVelocity = state.LinearVelocity;
        //float acceleration = input == Vector2.Zero ? Friction : Acceleration;
        float lerpWeight = Acceleration * delta;
        //if (PlayerController != null) _log.Information(lerpWeight.ToString());
        lerpWeight = Mathf.Clamp(lerpWeight, 0, 1);

        if (currentVelocity.Length() < ControlThreshold)
        {
            state.LinearVelocity = currentVelocity.Lerp(targetVelocity, /*lerpWeight*/ 0.85f);
        }
        else
        {
            //Коммент про LinearDump настройку в ProjectSettings.physics/2d/default_linear_damp (advanced settings)
            //Сейчас разная дистанция/время отталкивания в зависимости от изначального направления движения. Если ты в полете начал идти против взрыва -- ничего не происходит.
            //state.ApplyCentralForce(input * Acceleration * 10f); 
        }
    }
    
    private Vector2 GetMovementInSecondFromInput()
    {
        Vector2 input = PlayerController.GetMovementInput();
        return input * MaxSpeed;
    }
    
    private void ExplodeFromMouse()
    {
        float ExplosionRadius = 150;
        float MaxExplosionForce = 500;
        
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 myPos = GlobalPosition;

        // 1. Находим вектор от эпицентра (мыши) к объекту
        Vector2 diff = myPos - mousePos;
    
        // 2. Считаем расстояние
        float distance = diff.Length();

        // Если мы за пределами радиуса взрыва — игнорируем
        if (distance >= ExplosionRadius) return;

        // 3. Вычисляем направление (нормализуем вектор)
        Vector2 direction = diff.Normalized();

        // 4. Считаем силу затухания (Linear Falloff)
        // distance / ExplosionRadius дает число от 0 до 1 (где 1 — это край)
        // Мы вычитаем это из 1, чтобы получить: 1 в центре, 0 на краю
        float powerFactor = 1.0f - (distance / ExplosionRadius);
    
        // Квадратичное затухание (более реалистичный взрыв)
        //powerFactor = powerFactor * powerFactor; 

        // 5. Применяем импульс
        Vector2 finalImpulse = direction * MaxExplosionForce * powerFactor;
        //LinearVelocity = Vector2.Zero; //Чтобы не учитывалась скорость от управления игроком. Но как учитывать скорость от других столкновений?
        ApplyCentralImpulse(finalImpulse);
    }
    
    // -----------------------------------------------------------------
    // -----------------------------------------------------------------
    // -----------------------------------------------------------------
    
    public void StatusEffect_OnClientApply(int clientId, int typeId, byte[] payload) => 
        Rpc(MethodName.StatusEffect_OnClientApplyRpc, clientId, typeId, payload);
    [Rpc(CallLocal = true)]
    private void StatusEffect_OnClientApplyRpc(int clientId, int typeId, byte[] payload)
    {
        ClientStatusEffects.AddStatusEffect(clientId, typeId, payload);
    }

    public void StatusEffect_OnClientRemove(int clientId) => Rpc(MethodName.StatusEffect_OnClientRemoveRpc, clientId);
    [Rpc(CallLocal = true)]
    private void StatusEffect_OnClientRemoveRpc(int clientId)
    {
        ClientStatusEffects.RemoveStatusEffect(clientId);
    }
    
}
