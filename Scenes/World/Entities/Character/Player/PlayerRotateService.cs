using System;
using Godot;

//TODO добавить аннотацию [EventBusListener] или типа того, чтобы автоматически подписывать соответствующиме методы сервиса на события шины
[GameService]
public class PlayerRotateService
{
    public PlayerRotateService()
    {
        EventBus.Subscribe<PlayerProcessEvent>(OnPlayerProcessEvent);
    }
    
    public void OnPlayerProcessEvent(PlayerProcessEvent playerProcessEvent) {
        RotateToMouse(playerProcessEvent.Player, playerProcessEvent.Delta);
    }
    
    public void RotateToMouse(Player player, double delta)
    {
        //Куда хотим повернуться
        double targetAngle = GetAngleToMouse(player);
        //На какой угол надо повернуться (знак указывает направление)
        double deltaAngleToTargetAngel = Mathf.AngleDifference(player.Rotation - Mathf.Pi / 2, targetAngle);
        //Только направление (-1, 0, 1)
        double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
        //Максимальная скорость поворота (за секунду)
        double rotationSpeedRad = Mathf.DegToRad(player.RotationSpeed);
        //Максимальная скорость поворота (за прошедшее время)
        rotationSpeedRad *= delta;
        //Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
        rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
        //Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
        rotationSpeedRad *= directionToTargetAngel;
        //Поворачиваемся на угол
        player.Rotation += rotationSpeedRad;
    }
    
    public double GetAngleToMouse(Player player)
    {
        // Получаем текущую позицию мыши
        var mousePos = player.GetGlobalMousePosition();
        // Вычисляем вектор направления от объекта к мыши
        var mouseDir = player.GlobalPosition.DirectionTo(mousePos);
        // Вычисляем направление от объекта к мыши
        return mouseDir.Angle();
    }
}