using Godot;
using KludgeBox.Events;

public readonly record struct PlayerGainXpEvent(Player Player, int Xp);
public readonly record struct PlayerProcessEvent(Player Player, double Delta);
public readonly record struct PlayerReadyEvent(Player Player);
public readonly record struct PlayerPhysicsProcessEvent(Player Player, double Delta);

public enum WheelEventType { WheelUp, WheelDown }
public readonly record struct PlayerMouseWheelInputEvent(Player Player, WheelEventType WheelEvent);

public readonly record struct PlayerBasicSkillUseEvent(Player Player);
public readonly record struct PlayerAdvancedSkillUseEvent(Player Player);

public record QueryEvent<T> //TODO вынести в общий класс
{
    public bool HasResult { get; private set; }
    public T Result { get; private set; }

    public void SetResult(T result)
    {
        HasResult = true;
        Result = result;
    }
}

public record PlayerGetRequiredXpQuery(Player Player) : QueryEvent<int>; 