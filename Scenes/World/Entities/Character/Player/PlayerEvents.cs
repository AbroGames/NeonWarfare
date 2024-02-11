
public readonly record struct PlayerGainXpEvent(Player Player, int Xp);
public readonly record struct PlayerProcessEvent(Player Player, double Delta);
public readonly record struct PlayerReadyEvent(Player Player);
public readonly record struct PlayerPhysicsProcessEvent(Player Player, double Delta);

public enum WheelEventType
{
    WheelUp,
    WheelDown
}
public readonly record struct PlayerMouseWheelInputEvent(Player Player, WheelEventType wheelEvent);
public readonly record struct PlayerBasicSkillUseEvent(Player Player);
public readonly record struct PlayerAdvancedSkillUseEvent(Player Player);
