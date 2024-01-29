using Godot;

namespace MicroSurvivors;

public static class Keys
{
    // Basic movement
    public static readonly StringName Up = "KeyUp";
    public static readonly StringName Down = "KeyDown";
    public static readonly StringName Left = "KeyLeft";
    public static readonly StringName Right = "KeyRight";
    
    // Mouse
    public static readonly StringName Attack = "MouseLeftClick";
    
    // Special
    public static readonly StringName CameraShift = "KeyCameraShift";
}