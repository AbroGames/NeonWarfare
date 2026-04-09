using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;

public class AiBattleControllerLogic : IAiControllerLogic
{

    public Vector2 Direction = Vec2(0, 0);
    
    public Vector2 GetMovementInput(Character character)
    {
        return Direction;
    }

    public Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.Position + Vector2.Up * 10;
    }
}