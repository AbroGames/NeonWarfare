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
    public string LogId = null;
    
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
        if (LogId != null) _log.Warning("{id}: {str}", LogId, str);
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
    public float GroundFriction = 2000.0f;
    public float AirFriction = 0.04f;
    
    //TODO Потестить изменение Force и Силу затухания (линейно и квадрат)
    public float ExplosionRadius = 300;
    public float MaxExplosionForce = 2000;
    
    private Vector2 _lastPredictionVelocity = Vector2.Zero;
    
    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        if (LogId != null) _log.Information($"");
        Vector2 input = Controlled ? GetMovementInput() : Vec;
        
        PhysicsPrediction prediction = CalculateAnalyticMotion(
            _lastPredictionVelocity,
            input,
            Force,
            Mass,
            GroundFriction,
            AirFriction, // Важно: сюда передавать "чистый" коэффициент, без умножения на V^2
            state.Step // это delta внутри IntegrateForces
        );

        // Применяем сразу финальные значения
        _lastPredictionVelocity = prediction.NewVelocity;
        state.LinearVelocity = prediction.PositionOffset / state.Step;

        if (LogId != null)
            _log.Information($"{LogId} - " +
                $"Position: {N1(Position)}, " +
                $"Position Offset: {N1(prediction.PositionOffset)}, " +
                $"Velocity: {LinearVelocity.Length()}, " + 
                $"Prediction Velocity: {_lastPredictionVelocity.Length()}, ");
        
        float maxSpeed = 1000.0f; // Максимально разумная скорость для вашей игры
        if (state.LinearVelocity.LengthSquared() > maxSpeed * maxSpeed)
        {
            //state.LinearVelocity = state.LinearVelocity.Normalized() * maxSpeed; //TODO ВЕРНУТЬ
            if (LogId != null) _log.Information($"{LogId} - " + $"MAX SPEED");
        }
    }
    
    public struct PhysicsPrediction
    {
        public Vector2 NewVelocity;
        public Vector2 PositionOffset; // Насколько сдвинемся за этот кадр
    }

    /// <summary>
    /// Здесь аналитический расчет сил, вместо ApplyCentralForce. Т.к. в ApplyCentralForce сила применяется на 16/33 мс, и может быть чрезмерна.
    /// Например, трение воздуха за эти 33 мс остановит игрока и даже отправит его в обратную сторону.
    /// Старые формулы для расчета, в текущем методе реализованы они же, но аналитически и с максимальной точностью:
    /// Vector2 engineForce = input * Force;
    /// Vector2 groundFrictionForce = -LinearVelocity.Normalized() * Mass * GroundFriction;
    /// Vector2 airFrictionForce = -LinearVelocity.Normalized() * (LinearVelocity.LengthSquared() * AirFriction); // Мб не ^2, а ^4, т.к. аркада
    /// Vector2 frictionForce = groundFrictionForce + airFrictionForce;
    /// ApplyCentralForce(engineForce + frictionForce);
    /// Также тут были костыли, про то, что при input = Vector.Zero мы применяем engineForce против двидения (помогаем остановиться быстрее)
    /// И в последнем степе применяем идеально выверенное engineForce для остановки в ноль.
    /// Если потребуется восстановить как всё выглядело, то можно посмотреть коммит db459685 on 06.12.2025 at 19:24
    /// В этом коммите обычный метод приложения сил был заменен на аналитический, а также включена опция custom_integrator, вместо ванильног расчета
    /// </summary>
    public PhysicsPrediction CalculateAnalyticMotion(
        Vector2 currentVelocity,
        Vector2 input,
        float force,
        float mass,
        float groundFriction,
        float airFriction,
        double delta)
    {
        float dt = (float)delta;
    
        // --- ШАГ 1: Линейные силы (Векторное сложение) ---
        // Здесь мы определяем, КУДА будет направлена скорость после работы мотора и трения земли.
        
        // 1. Сила мотора (вектор)
        Vector2 engineForceVec = input * force;
        
        // 2. Сила трения земли (вектор против текущей скорости)
        // Важно: рассчитываем "идеальное" трение, но ограничиваем его, чтобы не уйти назад (как обсуждали раньше)
        Vector2 frictionDir = currentVelocity.LengthSquared() > 0.0001f ? -currentVelocity.Normalized() : Vector2.Zero;
        Vector2 maxGroundFriction = frictionDir * (mass * groundFriction);
        
        // Импульс трения за кадр не должен превышать текущий импульс тела
        Vector2 impulseLimit = -(currentVelocity * mass) / dt; 
        
        Vector2 appliedGroundFriction;
        if (maxGroundFriction.LengthSquared() > impulseLimit.LengthSquared())
        {
            appliedGroundFriction = impulseLimit; // Полная остановка от трения
        }
        else
        {
            appliedGroundFriction = maxGroundFriction;
        }

        // 3. Промежуточная скорость (V_temp)
        // Это скорость, которая БЫЛА БЫ, если бы не было воздуха.
        // Тут меняется направление (дрифт).
        Vector2 velocityTemp = currentVelocity + (engineForceVec + appliedGroundFriction) / mass * dt;
        if (LogId != null)
            _log.Information($"{LogId} - " +
                             $"Temp Velocity: {velocityTemp.Length()}");


        // --- ШАГ 2: Сопротивление воздуха (Аналитическое скалярное решение) ---
        // Теперь мы берем длину V_temp и применяем к ней "неявный Эйлер" для квадратичного затухания.
        
        float tempSpeed = velocityTemp.Length();
        
        if (tempSpeed <= 0.0001f)
        {
            return new PhysicsPrediction { NewVelocity = Vector2.Zero, PositionOffset = Vector2.Zero };
        }

        // Решаем уравнение: v_new = v_temp - (k/m) * v_new^2 * dt
        // Приводим к: A*x^2 + B*x + C = 0
        float k = airFriction; // Коэффициент (без V^2, просто константа)
        
        float A = (k / mass) * dt;
        float B = 1.0f;
        float C = -tempSpeed; // Обратите внимание: минус tempSpeed

        float finalSpeed = 0;

        if (Mathf.IsZeroApprox(A))
        {
            finalSpeed = tempSpeed;
        }
        else
        {
            // Дискриминант: D = b^2 - 4ac
            // C у нас отрицательное, значит -4ac будет положительным. Корни всегда есть.
            float discriminant = B * B - 4 * A * C;
            // Нам нужен положительный корень: (-B + sqrt(D)) / 2A
            finalSpeed = (-B + Mathf.Sqrt(discriminant)) / (2 * A);
        }
        
        if (LogId != null)
            _log.Information($"{LogId} - " +
                             $"Final Velocity: {finalSpeed}");

        // --- ШАГ 3: Сборка результата ---
        
        // Берем направление от Векторного шага, а длину от Аналитического шага
        Vector2 finalVelocity = velocityTemp.Normalized() * finalSpeed;

        // Расчет позиции (метод трапеций для точности)
        Vector2 positionOffset = (currentVelocity + finalVelocity) * 0.5f * dt;

        return new PhysicsPrediction 
        { 
            NewVelocity = finalVelocity, 
            PositionOffset = positionOffset 
        };
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
        _lastPredictionVelocity += finalImpulse / Mass;
        //ApplyCentralImpulse(finalImpulse);
    }
}
