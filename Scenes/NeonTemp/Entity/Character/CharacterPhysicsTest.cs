using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character;

//TODO After test del this class and associated scene, del trash from MapSurface
public partial class CharacterPhysicsTest : RigidBody2D
{
    
    [Child] public Sprite2D Sprite { get; private set; }
    [Child] public Area2D HitBox { get; private set; }
    
    public Vector2 Vec;
    public bool Controlled = false;
    
    public float MaxSpeed = 200.0f;
    public float Acceleration = 10.0f;
    //public float Friction = 150.0f;
    
    private float ControlThreshold => MaxSpeed * 1.1f;
    
    public override void _Ready()
    {
        //ApplyCentralForce(Vec);
        //ApplyCentralImpulse(Vec);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionReleased(Keys.AttackPrimary))
        {
            ExplodeFromMouse();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        //MoveAndCollide(GetMovementInSecondFromInput() * (float) delta);
        //Velocity = GetMovementInSecondFromInput(); MoveAndSlide();

        //MoveAndCollide(Velocity * (float) delta);
        //MoveAndSlide();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        float delta = state.Step;
        Vector2 input = Controlled ? GetMovementInput() : Vec;
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
    
    private Vector2 GetMovementInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
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
}
