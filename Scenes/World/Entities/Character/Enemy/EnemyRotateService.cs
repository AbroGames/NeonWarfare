using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

[GameService]
public class EnemyRotateService
{
    
    [EventListener]
    public void OnEnemyProcessEvent(EnemyProcessEvent enemyProcessEvent)
    {
        var (enemy, delta) = enemyProcessEvent;
        
        //TODO дублируется с Player. Вынести в Utils?
        //Куда хотим повернуться
        double targetAngle = GetAngleToTarget(enemy);
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(enemy.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(enemy.RotationSpeed);
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        enemy.Rotation += rotationSpeedRad;
    }
    
    private double GetAngleToTarget(Enemy enemy) //TODO почти дублируется с Player. Вынести в Utils?
    {
        // Получаем текущую позицию мыши
        var targetPos = enemy.Target.GlobalPosition;
        // Вычисляем вектор направления от объекта к мыши
        var targetDir = enemy.GlobalPosition.DirectionTo(targetPos);
        // Вычисляем направление от объекта к мыши
        return targetDir.Angle();
    }
}