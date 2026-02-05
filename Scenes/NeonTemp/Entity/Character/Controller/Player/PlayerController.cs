using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

public class PlayerController : IController
{
    private readonly PhysicsCalculator _physicsCalculator = new();
    
    private long _nextOrderId;
    [Logger] private ILogger _log;

    public PlayerController()
    {
        Di.Process(this);
    }

    public void OnPhysicsProcess(double delta, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler)
    {
        //TODO Here or in OnUnhandledInput
        if (!controlBlockerHandler.IsSkillsBlocked())
        {
            //TODO Input.IsActionPressed(action);
        }
    }

    public void OnIntegrateForces(PhysicsDirectBodyState2D state, Character character, CharacterSynchronizer synchronizer, ControlBlockerHandler controlBlockerHandler, Vector2? teleportTask)
    {
        if (teleportTask.HasValue)
        {
            character.Position = teleportTask.Value;
            state.Transform = new Transform2D(state.Transform.Rotation, teleportTask.Value);
            state.LinearVelocity = Vector2.Zero;
            //TODO Телепорт таск дублируется. Вынести куда-то? И сделать параметр teleportTask в функции OnPhysicsProcess в интерфейсе тоже
            //TODO По хорошему мы должны отправить новый SendMovement, но из-за ненадежности передачи и этого не достаточно
            //TODO А зачем вообще этот метод на клиенте? Пусть в обычном порядке выполнится логика телепорта из DistanceForTeleport ветки
            //TODO Надо при спауне юнитов по хорошему? Т.к. спаун по надежному каналу и там же телепорт. В коммент это всё.
            //TODO В идеале мы должны при отправке команды телепорта отправить последний _nextOrderId, чтобы все пакеты со старыми корами гарантировано заигнорились
            return;
        }
        
        Vector2 movementInput = controlBlockerHandler.IsMovementBlocked() ? Vector2.Zero : GetMovementInput(character);
        _physicsCalculator.OnIntegrateForces(state, character, movementInput);
        Vector2 movementInSec = _physicsCalculator.LastPredictionVelocity;
        
        if (!controlBlockerHandler.IsRotatingBlocked())
        {
            character.RotateToTarget(GetGlobalRotatePosition(character), GetRotationSpeed(character), state.Step);
        }
        
        synchronizer.Controller_SendMovement(new IController.MovementData(
            orderId: _nextOrderId++,
            positionX: character.Position.X, //TODO Сравнить это значение с полученным значение из _physicsCalculator, если отчаются, то попробовать поменять на _physicsCalculator
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

    public void OnImpulse(Character character, Vector2 impulse)
    {
        _physicsCalculator.AddImpulse(impulse, character.Mass);
    }
    
    protected virtual Vector2 GetMovementInput(Character character)
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
    protected virtual Vector2 GetGlobalRotatePosition(Character character)
    {
        return character.GetGlobalMousePosition();
    }

    //TODO Переделать в GetForce
    protected virtual double GetMovementSpeed(Character character)
    {
        return character.StatsClient.MovementSpeed;
    }
    
    protected virtual double GetRotationSpeed(Character character)
    {
        return character.StatsClient.RotationSpeed;
    }
}