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
        return Services.Rand.UnitVector * 0.3f;
    }
    
    protected override Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.Position + Services.Rand.UnitVector * 10;
    }

    protected override double GetMovementSpeed(Character character)
    {
        return character.Stats.MovementSpeed;
    }
    
    protected override double GetRotationSpeed(Character character)
    {
        return character.Stats.RotationSpeed;
    }
}