using System;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemyRotateComponent : Node
{
    private ServerEnemy _parent;
    
    public override void _Ready()
    {
        _parent = GetParent<ServerEnemy>();
    }

    //TODO вынести в отдельный Utils класс / common component, дублируется с ClientRotate
    public override void _Process(double delta)
    {
        //Куда хотим повернуться
        double targetAngle = GetAngleToTarget();
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(_parent.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(_parent.RotationSpeed); //TODO RotationSpeed хранить в компоненте? Или лямбда для получения?
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        _parent.Rotation += (float) rotationSpeedRad;
    }
    
    private double GetAngleToTarget()
    {
        // Получаем текущую позицию игрока
        var targetPos = GetClosestPlayer();
        // Вычисляем вектор направления от объекта к игроку
        var targetDir = _parent.DirectionTo(targetPos);
        // Вычисляем направление от объекта к игроку
        return targetDir.Angle();
    }

    //TODO поменять на более сложный алгоритм смены цели, чтобы врага нельзя было таскать туда-сюда. Учесть возможность выхода всех игроков с сервера (например, падение сети).
    private ServerPlayer GetClosestPlayer()
    {
        ServerPlayer closestPlayer = null;
        double closestDistance = double.MaxValue;
        
        foreach (ServerPlayer player in ServerRoot.Instance.Game.World.GetPlayers())
        {
            double distance = _parent.DistanceTo(player);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }
}
