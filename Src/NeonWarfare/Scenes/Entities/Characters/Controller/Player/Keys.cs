using Godot;

namespace NeonWarfare.Scenes.Entity.Characters.Controller.Player;

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
    
    // UI
    //TODO Вынести куда-то? Оставить здесь только действия связанные с управлением Character-ом?
    public static readonly StringName UiUp = "ui_up";
    public static readonly StringName UiDown = "ui_down";
    public static readonly StringName UiLeft = "ui_left";
    public static readonly StringName UiRight = "ui_right";
}