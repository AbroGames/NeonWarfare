﻿using Godot;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

/// <summary>
/// Эта штука должна управлять всеми основными компонентами врагов. Она же отвечает за поиск пути.
/// </summary>
public partial class ServerEnemyAiComponent : Node
{
    private const double PathRecalculationTimeoutSec = 1; 
    
    private ServerEnemy _parent;
    private ServerEnemyMovementComponent _movementComponent;
    private ServerEnemyTargetComponent _targetComponent;
    private RotateComponent _rotateComponent;
    
    // Так как игроки двигаются, нам нужно постоянно пересчитывать путь до них.
    // В будущем, возможно, придется оптимизировать эту часть (например, сокращать интервал поиска пути при приближении к цели
    // и увеличивать его на больших расстояниях).
    private AutoCooldown _recalculatePathCooldown = new(PathRecalculationTimeoutSec);


    private Vector2? _ignoreFinalPosition = null;
    public override void _Ready()
    {
        _parent = GetParent<ServerEnemy>();
        _movementComponent = _parent.GetChild<ServerEnemyMovementComponent>();
        _targetComponent = _parent.GetChild<ServerEnemyTargetComponent>();
        _rotateComponent = _parent.GetChild<RotateComponent>();
        
        _parent.NavigationAgent.MaxSpeed = (float)_parent.MovementSpeed; // Пока ничего не делает, потенциально может быть использовано для избегания движущихся препятствий
        
        // Телепортируем юнита в конечную точку, когда он её почти достиг
        _parent.NavigationAgent.TargetReached += () =>  
        {
            _parent.GlobalPosition = _parent.NavigationAgent.TargetPosition;
        };
        
        _recalculatePathCooldown.ActionWhenReady += RecalculatePath;

        ConfigureComponents();
    }

    /// <summary>
    /// В теории нам не всегда надо вторгаться в работу сразу всех компонентов. Тут будет происходить настройка только нужных компонентов.
    /// </summary>
    protected virtual void ConfigureComponents()
    {
        _movementComponent.CustomMovementDirectionProvider = GetMovementDirection;
        _rotateComponent.GetTargetGlobalPositionFunc = GetTargetGlobalPositionFunc;
    }

    protected virtual Vector2? GetMovementDirection()
    {
        var finalPos = _parent.NavigationAgent.GetFinalPosition();
        if (finalPos != _ignoreFinalPosition)
        {
            if (_parent.GlobalPosition.DistanceTo(finalPos) <= _parent.NavigationAgent.TargetDesiredDistance)
            {
                _parent.GlobalPosition = finalPos;
                _ignoreFinalPosition = finalPos;
                _parent.NavigationAgent.TargetPosition = finalPos;
            }
        }

        var direction = GetAvailableMovement().Normalized();

        return direction;
    }

    protected virtual void RecalculatePath()
    {
        _parent.NavigationAgent.TargetPosition = _targetComponent.Target.GlobalPosition;
    }

    protected virtual Vector2? GetTargetGlobalPositionFunc()
    {
        // Враг смотрит на цель, если цель находится в пределах досягаемости
        if (_targetComponent.Target.DistanceTo(_parent) <= _parent.GetEnemyReachRange())
            return _targetComponent.Target.GlobalPosition;

        // либо смотрит в направлении движения, если цель за пределами досягаемости
        return _parent.NavigationAgent.GetNextPathPosition();
    }

    public override void _PhysicsProcess(double delta)
    {
        _recalculatePathCooldown.Update(delta);
    }
    
    private Vector2 GetAvailableMovement()
    {
        var nextPos = _parent.NavigationAgent.GetNextPathPosition();
        var direction = nextPos - _parent.GlobalPosition;

        return direction;
    }
}