using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;

public class AiBattleControllerLogic : IAiControllerLogic
{
    public Vector2 GetMovementInput(Character character)
    {
        return Vec2(-1, 0);
    }

    public Vector2 GetGlobalRotatePosition(Character character)
    {
        return Vector2.Zero;
    }
}