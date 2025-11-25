using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public class PlayerController : ICharacterController
{

    public readonly ControlBlockerHandler ControlBlockerHandler = new();

    public bool IsActionPressed(StringName action)
    {
        //TODO Разделить на keyboard / mouse. Проверку isBlocked(). И в идеале не просто кнопку проверять, а действие (чтобы боты могли так скиллы юзать)
        return Input.IsActionPressed(action);
        //TODO При dead просто не надо опрашивать контроллер. Использовать _unhandledInput + Input.IsActionPressed для скиллов зажатых. Везде _unhandledInput? Но надо проверить отлов released при свернутом окне.
    }

    public Vector2? GetMousePosition(CanvasItem canvas)
    {
        if (ControlBlockerHandler.IsMouseKeyBlocked()) return null;
        return canvas.GetLocalMousePosition();
    }
    
    public Vector2 GetMovementInput()
    {
        if (ControlBlockerHandler.IsKeyboardKeyBlocked()) return Vector2.Zero;
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}