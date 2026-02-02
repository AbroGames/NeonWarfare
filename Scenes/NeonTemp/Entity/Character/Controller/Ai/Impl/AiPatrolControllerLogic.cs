using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;

public class AiPatrolControllerLogic : IAiControllerLogic
{
    public Vector2 GetMovementInput(Character character)
    {
        return Services.Rand.UnitVector;
    }

    public Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.Position + Services.Rand.UnitVector * 10;
    }

    public double GetMovementSpeed(Character character)
    {
        return character.Stats.MovementSpeed * 0.2;
    }

    public double GetRotationSpeed(Character character)
    {
        return character.Stats.RotationSpeed * 0.5;
    }
}