using Godot;

namespace NeonWarfare.Scripts.Content;

public static class Keys
{
    // Basic movement
    public static readonly StringName Up = "KeyUp";
    public static readonly StringName Down = "KeyDown";
    public static readonly StringName Left = "KeyLeft";
    public static readonly StringName Right = "KeyRight";
    
    // Mouse
    public static readonly StringName AttackPrimary = "KeyAttackPrimary";
    public static readonly StringName AttackSecondary = "KeyAttackSecondary";
    public static readonly StringName WheelUp = "WheelUp";
    public static readonly StringName WheelDown = "WheelDown";
    
    // Special
    public static readonly StringName CameraShift = "KeyCameraShift";
    public static readonly StringName Flashlight = "KeyFlashlight";
    
    // Abilities
    public static readonly StringName AbilityBasic = "KeyAbilityBasic";
    public static readonly StringName AbilityAdvanced = "KeyAbilityAdvanced";
    
    // UI
    public static readonly StringName Chat = "KeyChat";
    public static readonly StringName Enter = "KeyEnter";
    public static readonly StringName Cancel = "KeyCancel";
    public static readonly StringName Debug = "KeyDebug";
    public static readonly StringName ListPlayers = "KeyListPlayers";
    public static readonly StringName UiUp = "ui_up";
    public static readonly StringName UiDown = "ui_down";
    public static readonly StringName UiLeft = "ui_left";
    public static readonly StringName UiRight = "ui_right";
}
