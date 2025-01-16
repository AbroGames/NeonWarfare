using System;
using Godot;
using NeonWarfare;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayerRotateComponent : Node
{
    private ClientPlayer _parent;
    
    public override void _Ready()
    {
        _parent = GetParent<ClientPlayer>();
    }

    public override void _Process(double delta)
    {
        //Куда хотим повернуться
        double targetAngle = GetAngleToMouse();
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(_parent.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(_parent.RotationSpeed);
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        _parent.Rotation += (float) rotationSpeedRad;
    }

    private double GetAngleToMouse()
    {
        // Получаем текущую позицию мыши
        var mousePos = _parent.GetGlobalMousePosition();
        // Вычисляем вектор направления от объекта к мыши
        var mouseDir = _parent.GlobalPosition.DirectionTo(mousePos);
        // Вычисляем направление от объекта к мыши
        return mouseDir.Angle();
    }
}
