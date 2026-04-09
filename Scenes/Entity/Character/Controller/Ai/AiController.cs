using Godot;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;

public class AiController : PlayerController
{

    private IAiControllerLogic _logic;

    public AiController(IAiControllerLogic logic)
    {
        Di.Process(this);
        
        _logic = logic;
    }

    protected override Vector2 GetMovementInput(Character character)
    {
        return _logic.GetMovementInput(character);
    }
    
    protected override Vector2 GetGlobalRotatePosition(Character character)
    {
        return _logic.GetGlobalRotatePosition(character);
    }

    protected override double GetMovementSpeed(Character character)
    {
        return _logic.GetMovementSpeed(character);
    }
    
    protected override double GetRotationSpeed(Character character)
    {
        return _logic.GetRotationSpeed(character);
    }
}