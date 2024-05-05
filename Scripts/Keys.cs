﻿using Godot;

namespace NeonWarfare;

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
    
    // Abilities
    public static readonly StringName AbilityBasic = "KeyAbilityBasic";
    public static readonly StringName AbilityAdvanced = "KeyAbilityAdvanced";
}