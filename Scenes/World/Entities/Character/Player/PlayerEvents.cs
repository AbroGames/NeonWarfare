using Godot;
using KludgeBox.Events;

namespace NeoVector;

public readonly record struct PlayerProcessEvent(Player Player, double Delta) : IEvent;
public readonly record struct PlayerReadyEvent(Player Player) : IEvent;
public readonly record struct PlayerInputEvent(Player Player, InputEvent Event) : IEvent;
public readonly record struct PlayerPhysicsProcessEvent(Player Player, double Delta) : IEvent;

public enum WheelEventType { WheelUp, WheelDown }
public readonly record struct PlayerMouseWheelInputEvent(Player Player, WheelEventType WheelEvent) : IEvent;

public readonly record struct PlayerBasicSkillUseEvent(Player Player) : IEvent;
public readonly record struct PlayerAdvancedSkillUseEvent(Player Player) : IEvent;

public record PlayerGetRequiredXpQuery(Player Player) : QueryEvent<long>;

public readonly record struct PlayerDeathEvent(Player Player) : IEvent;

public readonly record struct PlayerAttackPrimaryEvent(Player Player) : IEvent;
public readonly record struct PlayerAttackSecondaryEvent(Player Player) : IEvent;