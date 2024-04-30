using System;
using Godot;
using KludgeBox.Net;

namespace NeoVector;

public partial class PlayerRotateComponent : Node
{
    public Player Player { get; private set; } 
    
    public override void _Ready()
    {
        Player = GetParent<Player>();
    }

    public override void _Process(double delta)
    {
        //Куда хотим повернуться
        double targetAngle = GetAngleToMouse();
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(Player.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(Player.RotationSpeed);
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        Player.Rotation += rotationSpeedRad;
    }

    private double GetAngleToMouse()
    {
        // Получаем текущую позицию мыши
        var mousePos = Player.GetGlobalMousePosition();
        // Вычисляем вектор направления от объекта к мыши
        var mouseDir = Player.GlobalPosition.DirectionTo(mousePos);
        // Вычисляем направление от объекта к мыши
        return mouseDir.Angle();
    }
}