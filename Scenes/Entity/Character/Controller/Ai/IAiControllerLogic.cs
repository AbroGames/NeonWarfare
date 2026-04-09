using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;

public interface IAiControllerLogic
{
    public Vector2 GetMovementInput(Character character);
    public Vector2 GetGlobalRotatePosition(Character character);
    
    public double GetMovementSpeed(Character character)
    {
        return character.Stats.MovementSpeed;
    }

    public double GetRotationSpeed(Character character)
    {
        return character.Stats.RotationSpeed;
    }
}