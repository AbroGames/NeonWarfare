using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public interface ICharacterController
{
    public bool IsActionPressed(StringName action);
    public Vector2? GetMousePosition(CanvasItem canvas);
    public Vector2 GetMovementInput();
}