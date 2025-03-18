using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class RotateComponent : Node
{
    
    public Func<Vector2?> GetTargetGlobalPositionFunc;
    public Func<double> GetRotationSpeedFunc;
    
    private Node2D _parent;    
    
    public override void _Ready()
    {
        _parent = GetParent<Node2D>();

        if (GetTargetGlobalPositionFunc == null || GetRotationSpeedFunc == null)
        {
            Log.Warning("GetTargetFunc and GetRotationSpeedFunc must be not null");
        }
    }

    public override void _Process(double delta)
    {
        // Получаем координаты цели, куда хотим повернуться
        Vector2? targetPosition = GetTargetGlobalPositionFunc.Invoke();
        double rotationSpeed = GetRotationSpeedFunc.Invoke();
        if (!targetPosition.HasValue || rotationSpeed == 0) return;
        
        //Куда хотим повернуться
        double targetAngle = GetAngleToTarget(targetPosition.Value);
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(_parent.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(rotationSpeed);
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        _parent.Rotation += (float) rotationSpeedRad;
    }
    
    private double GetAngleToTarget(Vector2 targetPosition)
    {
        // Вычисляем вектор направления от объекта к цели
        var targetDir = _parent.GlobalPosition.DirectionTo(targetPosition);
        // Вычисляем направление от объекта к цели
        return targetDir.Angle();
    }
}