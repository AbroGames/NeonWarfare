using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;

public class AiMoveControllerLogic : IAiControllerLogic
{

    public Vector2 TargetPosition = Vec2(0, 0);
    
    public Vector2 GetMovementInput(Character character)
    {
        return (TargetPosition - character.Position).Normalized();
    }

    public Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.Position + Vector2.Up * 10;
    }
}