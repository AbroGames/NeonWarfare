using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;
using Serilog;

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
    
    private float ControlThreshold => MaxSpeed * 1.1f;
    [Logger] private ILogger _log;
    
    public override void _Ready()
    {
        Di.Process(this);
        //ApplyCentralForce(Vec);
        //ApplyCentralImpulse(Vec);
    }

    private void LogForPlayer(float f) => LogForPlayer(f.ToString());
    private void LogForPlayer(double d) => LogForPlayer(d.ToString());
    private void LogForPlayer(Vector2 v) => LogForPlayer(v.ToString());
    private void LogForPlayer(string str)
    {
        if (Controlled) _log.Warning("{str}", str);
    }

    private string N1(Vector2 vec)
    {
        return $"({vec.X:N1};{vec.Y:N1})";
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionReleased(Keys.AttackPrimary))
        {
            ExplodeFromGlobalPosition(GetGlobalMousePosition());
        }
        if (@event.IsActionReleased(Keys.AttackSecondary))
        {
            ExplodeFromGlobalPosition(GlobalPosition + Vec2(-15, 0));
        }
    }
    
    //TODO При 5000, 500, 0.05 и 1 получаем MaxSpeed = 300. Надо найти значения Force и NewMass для диапазона скоростей 150-450.
    public float Force = 5000.0f;
    public float GroundFriction = 500.0f;
    public float AirFriction = 0.05f;
    public float NewMass = 1f;
    
    //TODO Потестить изменение Force и Силу затухания (линейно и квадрат)
    public float ExplosionRadius = 300;
    public float MaxExplosionForce = 20000;

    public override void _PhysicsProcess(double delta)
    {
        //MoveAndCollide(GetMovementInSecondFromInput() * (float) delta);
        //Velocity = GetMovementInSecondFromInput(); MoveAndSlide();

        //MoveAndCollide(Velocity * (float) delta);
        //MoveAndSlide();
        
        if (Controlled) _log.Information("");
        
        Vector2 input = Controlled ? GetMovementInput() : Vec;
        Vector2 targetVelocity = input * MaxSpeed;
        Vector2 currentVelocity = LinearVelocity;
        //Vector2 frictionForce = Force * (-LinearVelocity / MaxSpeed);
        
        
        Mass = NewMass;
        Vector2 engineForce = input * Force;
        Vector2 groundFrictionForce = -LinearVelocity.Normalized() * Mass * GroundFriction;
        Vector2 airFrictionForce = -LinearVelocity.Normalized() * (LinearVelocity.LengthSquared() * AirFriction); // Мб не ^2, а ^4, т.к. аркада
        Vector2 frictionForce = groundFrictionForce + airFrictionForce;
        Vector2 maxVelocity = CalculateTerminalVelocity(new Vector2(1, 0), Force, Mass, GroundFriction, AirFriction);
        
        
        // 1. Calculate the force required to stop the object completely in exactly this frame.
        // Formula: F = m * a = m * (v / t)
        // We use the magnitude because we only care about the force limit, direction is handled by friction itself.
        float maxStoppingForce = currentVelocity.Length() * Mass / (float)delta;

        // 2. Check if our calculated friction (e.g. from explosion speed) exceeds this limit.
        // Using LengthSquared for performance optimization.
        if (frictionForce.LengthSquared() > maxStoppingForce * maxStoppingForce)
        {
            // 3. Clamp the friction.
            // We keep the original direction of friction, but limit its power to maxStoppingForce.
            frictionForce = frictionForce.Normalized() * maxStoppingForce;
            if (Controlled) _log.Information($"FIX FRICTION: {frictionForce.Length()}");
        }
        
        
        if (input == Vector2.Zero)
        {
            // 1. Считаем полную силу, необходимую для остановки ровно за этот кадр (dt).
            // F = m * a  =>  F = m * (0 - V) / dt
            Vector2 totalForceToStop = -(LinearVelocity * Mass) / (float)delta;

            // 2. Часть этой работы уже делает трение (frictionForce).
            // Двигателю нужно компенсировать только разницу.
            // totalForce = engineForce + frictionForce  =>  engineForce = totalForce - frictionForce
            Vector2 requiredEngineForce = totalForceToStop - frictionForce;

            // 3. Проверяем, хватает ли нам мощности двигателя ("Force"), 
            // чтобы выдать такую "идеальную" силу.
            float maxForceSq = Force * Force;
    
            if (requiredEngineForce.LengthSquared() > maxForceSq)
            {
                // Слишком быстро едем. Двигатель не может остановить мгновенно.
                // Прикладываем МАКСИМАЛЬНУЮ силу в нужную сторону (тормозим "в пол").
                engineForce = requiredEngineForce.Normalized() * Force;
            }
            else
            {
                // Мы уже почти стоим. Сила, нужная для остановки, меньше макс. мощности.
                // Прикладываем ровно столько, сколько нужно для идеального нуля.
                engineForce = requiredEngineForce;
                if (maxVelocity.Length() * 1.1f < LinearVelocity.Length())
                {
                    if (Controlled) _log.Information($"TOO MUCH CUT: {Force} {Mass}");
                }
            }
        }
        
        Vector2 resultForce = engineForce + groundFrictionForce; //TODO КОСТЫЛЬ, ЧТОБЫ НЕ УЧИТЫВАТЬ airFrictionForce
        
        
        if (maxVelocity.Length() * 1.1f < LinearVelocity.Length())
        {
            //resultForce *= 0.1f; //TODO ВЕРНУТЬ? Ведь проблема не в этом была, а в сопротивлении воздуха
        }

        _frictionForce = frictionForce;
        _lastVelocity = currentVelocity;
        _lastPosition = Position;
        
        ApplyCentralForce(resultForce);
        if (Controlled) _log.Information(
            $"Position: {N1(Position)}, " +
            $"Velocity: {currentVelocity.Length()}, " +
            $"Max Velocity: {maxVelocity.Length()}, " +
            $"Force: {N1(resultForce)}, " +
            $"Engine Force: {N1(engineForce)}, " +
            $"Ground Force: {N1(groundFrictionForce)}, " +
            $"Air Force: {N1(airFrictionForce)}");
    }

    private Vector2 _frictionForce;
    private Vector2 _lastVelocity;
    private Vector2 _lastPosition;
    
    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        //if (Controlled) _log.Information($"Current: {N1(state.LinearVelocity)}, Last: {N1(_lastVelocity)}");
        //if (Controlled) _log.Information($"Sum: {state.LinearVelocity.Normalized() + _lastVelocity.Normalized()}");
        //if (Controlled) _log.Information($"Position: {N1(Position)}");
        if (_lastVelocity != Vector2.Zero && state.LinearVelocity.Normalized() + _lastVelocity.Normalized() == Vector2.Zero) 
        {
            //ApplyImpulse(-state.LinearVelocity);
            //state.LinearVelocity = Vector2.Zero;
            //Position = _lastPosition;
            if (Controlled) _log.Information($"ZERO");
        }
        
        // AIR FRICTION АНАЛИТИЧЕСКИ:
        float speed = LinearVelocity.Length();
        if (speed > 0.001f) // Чтобы не делить 0
        {
            // k_over_m = AirFriction / Mass  (если AirFriction не умножен на массу)
            // Если используешь Способ 1, то просто k = AirFriction
            float k = AirFriction; 
    
            // Магия математики: точное значение скорости после сопротивления воздуха за время delta
            float newSpeed = speed / (1.0f + (k / Mass) * speed * (float) state.Step);
    
            // Применяем новое значение скорости, сохраняя направление
            if (Controlled) _log.Information($"AIR FRICTION: {LinearVelocity.Length()} -> {newSpeed}");
            LinearVelocity = LinearVelocity.Normalized() * newSpeed;
        }
        
        
        float maxSpeed = 1000.0f; // Максимально разумная скорость для вашей игры
        if (state.LinearVelocity.LengthSquared() > maxSpeed * maxSpeed)
        {
            //state.LinearVelocity = state.LinearVelocity.Normalized() * maxSpeed; //TODO ВЕРНУТЬ
            if (Controlled) _log.Information($"MAX SPEED");
        }
    }
    
    public float CalculateNeededForce(float targetSpeed, float mass)
    {
        float frictionGround = mass * GroundFriction;
        float frictionAir = (targetSpeed * targetSpeed) * AirFriction;
        return frictionGround + frictionAir;
    }
    
    public Vector2 CalculateTerminalVelocity(Vector2 input, float force, float mass, float groundFriction, float airFriction)
    {
        // 1. Calculate the magnitude of the driving force
        float engineForceMag = input.Length() * force;

        // 2. Calculate the magnitude of constant ground friction
        float groundFrictionMag = mass * groundFriction;

        // 3. Check if the engine is strong enough to overcome ground friction
        // If not, the object won't move, or will stop completely.
        if (engineForceMag <= groundFrictionMag)
        {
            return Vector2.Zero;
        }

        // 4. Handle division by zero (if AirFriction is 0, speed is theoretically infinite)
        if (Mathf.IsZeroApprox(airFriction))
        {
            GD.PushWarning("AirFriction is zero, terminal velocity is infinite.");
            return input.Normalized() * float.MaxValue;
        }

        // 5. Solve for velocity magnitude: V = Sqrt((F_engine - F_ground) / F_air_coeff)
        float velocitySquared = (engineForceMag - groundFrictionMag) / airFriction;
        float terminalSpeed = Mathf.Sqrt(velocitySquared);

        // 6. Return velocity vector in the direction of input
        return input.Normalized() * terminalSpeed;
    }
    
    private Vector2 GetMovementInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
    private void ExplodeFromGlobalPosition(Vector2 explodePos)
    {
        Vector2 myPos = GlobalPosition;

        // 1. Находим вектор от эпицентра (мыши) к объекту
        Vector2 diff = myPos - explodePos;
    
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
