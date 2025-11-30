using System;
using Godot;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public interface IController
{
    [MessagePackObject(AllowPrivate = true)]
    public struct MovementData(
        long orderId,
        float positionX,
        float positionY,
        float rotation,
        float movementX,
        float movementY)
    {
        [Key(1)] public readonly long OrderId = orderId;
        [Key(2)] public readonly float PositionX = positionX;
        [Key(3)] public readonly float PositionY = positionY;
        [Key(4)] public readonly float Rotation = rotation;
        [Key(5)] public readonly float MovementX = movementX;
        [Key(6)] public readonly float MovementY = movementY;
    }

    public void OnPhysicsProcess(double delta, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler);
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler);
    public void OnReceivedMovement(Character character, CharacterSynchronizer synchronizer, MovementData movementData);
}