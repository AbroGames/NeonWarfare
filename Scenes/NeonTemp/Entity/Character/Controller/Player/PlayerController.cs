using System;
using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

public class PlayerController : IController
{

    public void OnPhysicsProcess(double delta, Character character, ControlBlockerHandler controlBlockerHandler)
    {
        if (!controlBlockerHandler.IsMovementBlocked())
        {
            character.MoveAndCollide(GetMovementInput() * (float) (character.Stats.MovementSpeed * delta));
        }
        
        if (!controlBlockerHandler.IsRotatingBlocked())
        {
            character.RotateToTarget(character.GetGlobalMousePosition(), character.Stats.RotationSpeed, delta);
        }

        //TODO Here or in OnUnhandledInput
        if (!controlBlockerHandler.IsSkillsBlocked())
        {
            //TODO Input.IsActionPressed(action);
        }
    }

    //TODO Надо проверить отлов released при свернутом окне.
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, ControlBlockerHandler controlBlockerHandler)
    {
        
    }
    
    private Vector2 GetMovementInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}