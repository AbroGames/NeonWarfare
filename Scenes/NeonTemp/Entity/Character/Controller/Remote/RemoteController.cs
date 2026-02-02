using System;
using Godot;
using KludgeBox.Core.Cooldown;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Remote;

public class RemoteController : IController
{
    
    private const double InertiaTime = 0.1;
    private const double DistanceForTeleport = 50;
    
    private readonly ManualCooldown _cooldownFromLastMovementDataUpdate = new(InertiaTime);
    private IController.MovementData? _lastMovementData;
    
    public void OnPhysicsProcess(double delta, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler) { }
    
    public void OnIntegrateForces(PhysicsDirectBodyState2D state, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler, Vector2? teleportTask)
    {
        if (teleportTask.HasValue)
        {
            state.Transform = new Transform2D(state.Transform.Rotation, teleportTask.Value);
            return;
        }
        
        if (_lastMovementData == null) return;
        if (_cooldownFromLastMovementDataUpdate.IsCompleted) return;
        _cooldownFromLastMovementDataUpdate.Update(state.Step);

        Vector2 lastPosition = Vec2(_lastMovementData.Value.PositionX, _lastMovementData.Value.PositionY);
        Vector2 lastMovement = Vec2(_lastMovementData.Value.MovementX, _lastMovementData.Value.MovementY);
        Vector2 targetPosition = lastPosition + lastMovement * (float) _cooldownFromLastMovementDataUpdate.ElapsedTime;
        Vector2 movement = targetPosition - character.Position;

        if (movement.Length() < DistanceForTeleport)
        {
            Vector2 positionOffset = movement; // Можно умножить на понижающий коэффициент, если требуется больше плавности.
            state.LinearVelocity = positionOffset / state.Step; // Деление, потому что LinearVelocity хранит скорость в секунду, а PositionOffset за кадр
        }
        else
        {
            state.Transform = new Transform2D(state.Transform.Rotation, targetPosition);
        }

        character.Rotation = _lastMovementData.Value.Rotation;
    }
    
    public void OnReceivedMovement(Character character, CharacterSynchronizer synchronizer, IController.MovementData movementData)
    {
        if (movementData.OrderId == 0) _lastMovementData = null;
        if (_lastMovementData != null && _lastMovementData?.OrderId >= movementData.OrderId) return;
        
        _lastMovementData = movementData;
        _cooldownFromLastMovementDataUpdate.Restart();
    }
    
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler) { }
    
    /// <summary>
    /// Импульс всегда инициируется с сервера. Например, при помощи взрыва.<br/>
    /// Если мы получили данные о взрыве (учитывая пинг), то мы получили и данные о новой позиции юнита.
    /// Поэтому реализовывать этот метод не требуется.
    /// </summary>
    public void OnImpulse(Character character, Vector2 impulse) { }
}