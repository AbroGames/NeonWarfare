using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

public class PhysicsCalculator
{
    
    //TODO При 5000, 500, 0.05 и 1 получаем MaxSpeed = 300. Надо найти значения Force и NewMass для диапазона скоростей 150-450.
    //TODO Использовать CalculateTerminalVelocity для расчётов без тестов
    
    //TODO По сравнению со старым управлением, это ощущается очень резиновым, будто на льду 
    //TODO Но изменение AirFriction как будто не помогает, влияет только на Макс скорость.
    //TODO Ну и в целом если я смогу добиться быстрой остановки управлением, то перестанут работать взрывы.
    //TODO Мб всё таки сделать костыль который при превышении макс скорости режет управление на 50-90% ? Типа ты не касаешься земли.
    //TODO Или удаляет GroundFriction совсем. А в обычной ситуации завысить его.
    //TODO Можно через функцию аналогично максимальной скорости рассчитать текущий лаг при развороте, и посчитать для других значений: лаг при развороте, макс скорость, дистанцию отбрасывания, макс. скорость при отбрасывании 
    private readonly float _force = 5000.0f; //TODO Вычислять динамически
    private const float GroundFriction = 2000.0f;
    private const float AirFriction = 0.04f;
    private const float MaxSpeed = 1000.0f; // Максимально разумная скорость для вашей игры

    public Vector2 LastPredictionVelocity { get; private set; } = Vector2.Zero;
    
    //TODO Насколько эта функция быстрая? Стоит оптимизировать? Нагрузка на сервер 20-60% TPS, на клиент 3-15% TPS при кол-ве юнитов 10-300
    //TODO Как будто просчёт коллизий должен быть абсолютно одинаковым и там и там, почему сервер потребляет больше?
    //TODO Потому что на сервере юниты пытаются ехать, а на клиент они передают нулевую скорость?
    //TODO Но для 300 стоячих юнитов нагрузка на сервер тоже выше: 33% против 14%. При этом каждые 2-4 секунды прыжок до 70%. Для 10 юнитов нагрузка одинаковая: 4%.
    //TODO Надо подебажить и попрофайлить, попытаться понять в чём дело.
    public void OnIntegrateForces(PhysicsDirectBodyState2D state, Character character, Vector2 movementInput)
    {
        //TODO Из-за того, что клиент и сервер считают физику только чатси юнитов, масса как будто больше не работает при таранах и ничего не делает. Force по прежнему работает.
        //TODO На большой скорости (например, текущие настройки) игрок проскакивает сквозь врага. Скорее всего из-за того, что игрок у себя таранит врага, но на сервере враг таранится медленней и у игрока происходит телепорт врага в старую позицию.
        //TODO Потенциально может помочь увеличение дистанции ТП, но это негативно повлияет на все остальные кейсы рассинхрона.
        //TODO Создать задачу и потестить. Понять что делать с физикой. Мб не нужен Force параметр игроку, а только конечная скорость, которая преобразуется в Force.
        // if (Net.IsClient())
        // {
        //     character.Mass = 10;
        //     character.Controller.ForceCoef = 5;
        // }
        
        PhysicsPrediction prediction = CalculateAnalyticMotion(
            LastPredictionVelocity,
            movementInput,
            _force * character.Controller.ForceCoef,
            character.Mass,
            GroundFriction,
            AirFriction, // Важно: сюда передавать "чистый" коэффициент, без умножения на V^2
            state.Step // это delta внутри IntegrateForces
        );
        
        // Применяем сразу финальные значения
        LastPredictionVelocity = prediction.NewVelocity;
        state.LinearVelocity = prediction.PositionOffset / state.Step; // Деление, потому что LinearVelocity хранит скорость в секунду, а PositionOffset за кадр
        
        if (state.LinearVelocity.LengthSquared() > MaxSpeed * MaxSpeed)
        {
            //state.LinearVelocity = state.LinearVelocity.Normalized() * MaxSpeed; //TODO ВЕРНУТЬ
        }
    }

    /// <summary>
    /// Здесь аналитический расчет сил, вместо ApplyCentralForce. Т.к. в ApplyCentralForce сила применяется на 16/33 мс, и может быть чрезмерна.
    /// Например, трение воздуха за эти 33 мс остановит игрока и даже отправит его в обратную сторону.
    /// Старые формулы для расчета, в текущем методе реализованы они же, но аналитически и с максимальной точностью:
    /// <code>
    /// Vector2 engineForce = input * Force;
    /// Vector2 groundFrictionForce = -LinearVelocity.Normalized() * Mass * GroundFriction;
    /// Vector2 airFrictionForce = -LinearVelocity.Normalized() * (LinearVelocity.LengthSquared() * AirFriction);
    /// Vector2 frictionForce = groundFrictionForce + airFrictionForce;
    /// ApplyCentralForce(engineForce + frictionForce);
    /// </code>
    /// Также тут были костыли, про то, что при input = Vector.Zero мы применяем engineForce против движения (помогаем остановиться быстрее).
    /// И в последнем степе применяем идеально выверенное engineForce для остановки в ноль.
    /// Если потребуется восстановить как всё выглядело, то можно посмотреть коммит db459685 on 06.12.2025 at 19:24.
    /// В этом коммите обычный метод приложения сил был заменен на аналитический, а также включена опция custom_integrator, вместо ванильного расчета.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private PhysicsPrediction CalculateAnalyticMotion(
        Vector2 currentVelocity,
        Vector2 input,
        float force,
        float mass,
        float groundFriction,
        float airFriction,
        float delta)
    {
    
        // --- ШАГ 1: Линейные силы (Векторное сложение) ---
        // Здесь мы определяем, КУДА будет направлена скорость после работы мотора и трения земли.
        
        // 1. Сила мотора (вектор)
        Vector2 engineForceVec = input * force;
        
        // 2. Сила трения земли (вектор против текущей скорости)
        // Важно: рассчитываем "идеальное" трение, но ограничиваем его, чтобы не уйти назад (как обсуждали раньше)
        Vector2 frictionDir = currentVelocity.LengthSquared() > 0.0001f ? -currentVelocity.Normalized() : Vector2.Zero;
        Vector2 maxGroundFriction = frictionDir * (mass * groundFriction);
        
        // Импульс трения за кадр не должен превышать текущий импульс тела
        Vector2 impulseLimit = -(currentVelocity * mass) / delta; 
        
        Vector2 appliedGroundFriction = maxGroundFriction.LengthSquared() > impulseLimit.LengthSquared() ? 
            impulseLimit : // Полная остановка от трения
            maxGroundFriction;

        // 3. Промежуточная скорость (V_temp)
        // Это скорость, которая БЫЛА БЫ, если бы не было воздуха.
        // Тут меняется направление (дрифт).
        Vector2 velocityTemp = currentVelocity + (engineForceVec + appliedGroundFriction) / mass * delta;

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
        
        float A = (k / mass) * delta;
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

        // --- ШАГ 3: Сборка результата ---
        
        // Берем направление от Векторного шага, а длину от Аналитического шага
        Vector2 finalVelocity = velocityTemp.Normalized() * finalSpeed;

        // Расчет позиции (метод трапеций для точности, т.к. при линейном изменении скорости,
        // можем считать, расстояние будет пройдено со средней скоростью между начальной и конечной)
        Vector2 positionOffset = (currentVelocity + finalVelocity) * 0.5f * delta;

        return new PhysicsPrediction 
        { 
            NewVelocity = finalVelocity, 
            PositionOffset = positionOffset 
        };
    }

    public void AddImpulse(Vector2 impulse, float mass)
    {
        LastPredictionVelocity += impulse / mass;
    }

    /// <summary>
    /// Метод не используется в игре. Нужен для расчёта финальной скорости при балансе параметров физики.
    /// Так же его можно использовать для расчёта MaxSpeed в характеристиках игрока
    /// </summary>
    private Vector2 CalculateTerminalVelocity(Vector2 input, float force, float mass, float groundFriction, float airFriction)
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
    
    private struct PhysicsPrediction
    {
        public Vector2 NewVelocity; // Скорость на новой позиции в конце кадра
        public Vector2 PositionOffset; // Расстояние, на которое сдвинемся за этот кадр
    }
}