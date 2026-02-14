using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;

public class AiObserveControllerLogic : IAiControllerLogic
{
    public Vector2 GetMovementInput(Character character)
    {
        return Vector2.Zero;
    }

    public Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.Position + Services.Rand.UnitVector * 10;
    }
}