using System;
using System.Diagnostics;
using Godot;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Remote;

public class RemoteController : IController
{
    
    private const double InertiaTime = 0.1;
    private const double DistanceForTeleport = 50;
    
    private readonly Stopwatch _fromLastMovementDataUpdate = new();
    private IController.MovementData? _lastMovementData;
    
    public void OnPhysicsProcess(double delta, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler)
    {
        if (_lastMovementData == null) return;
        if (_fromLastMovementDataUpdate.Elapsed.TotalSeconds >= InertiaTime) return;

        Vector2 lastPosition = Vec2(_lastMovementData.Value.PositionX, _lastMovementData.Value.PositionY);
        Vector2 lastMovement = Vec2(_lastMovementData.Value.MovementX, _lastMovementData.Value.MovementY);
        Vector2 targetPosition = lastPosition + lastMovement * (float) _fromLastMovementDataUpdate.Elapsed.TotalSeconds;
        Vector2 movement = targetPosition - character.Position;
        
        if (movement.Length() < DistanceForTeleport) character.MoveAndCollide(movement);
        else character.Position = targetPosition;
        
        character.Rotation = _lastMovementData.Value.Rotation;
    }
    
    public void OnReceivedMovement(Character character, CharacterSynchronizer synchronizer, IController.MovementData movementData)
    {
        if (movementData.OrderId == 0) _lastMovementData = null;
        if (_lastMovementData != null && _lastMovementData?.OrderId >= movementData.OrderId) return;
        
        _lastMovementData = movementData;
        _fromLastMovementDataUpdate.Restart();
    }
    
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler) { }
}