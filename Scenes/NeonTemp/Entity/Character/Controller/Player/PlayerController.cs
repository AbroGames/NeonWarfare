using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

public class PlayerController : IController
{
    private long _nextOrderId;
    [Logger] private ILogger _log;

    public PlayerController()
    {
        Di.Process(this);
    }

    public void OnPhysicsProcess(double delta, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler)
    {
        Vector2 movementInSec = Vec2();
        if (!controlBlockerHandler.IsMovementBlocked())
        {
            movementInSec = GetMovementInput(character) * (float) GetMovementSpeed(character);
            character.MoveAndCollide(movementInSec * (float) delta);
        }
        
        if (!controlBlockerHandler.IsRotatingBlocked())
        {
            character.RotateToTarget(GetGlobalRotatePosition(character), GetRotationSpeed(character), delta);
        }

        //TODO Here or in OnUnhandledInput
        if (!controlBlockerHandler.IsSkillsBlocked())
        {
            //TODO Input.IsActionPressed(action);
        }
        
        synchronizer.Controller_SendMovement(new IController.MovementData(
            orderId: _nextOrderId++,
            positionX: character.Position.X,
            positionY: character.Position.Y,
            rotation: character.Rotation,
            movementX: movementInSec.X,
            movementY: movementInSec.Y));
    }

    //TODO Надо проверить отлов released при свернутом окне.
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler)
    {
        
    }
    
    public void OnReceivedMovement(Character character, CharacterSynchronizer synchronizer, IController.MovementData movementData)
    {
        _log.Error("Method {method} is not valid in {class}", nameof(OnReceivedMovement), GetType().Name);
    }
    
    protected virtual Vector2 GetMovementInput(Character character)
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
    protected virtual Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.GetGlobalMousePosition();
    }

    protected virtual double GetMovementSpeed(Character character)
    {
        return character.StatsClient.MovementSpeed;
    }
    
    protected virtual double GetRotationSpeed(Character character)
    {
        return character.StatsClient.RotationSpeed;
    }
}