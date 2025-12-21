using Godot;
using NeonWarfare.Scenes.World.Services;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;

public record PathfindingData(float CharacterRadius, float MaxMovementSpeed);

public class Pathfinder
{
    private Character _character;
    private NavigationAgent2D _navigationAgent;
    private NavigationService _navigationService;
    private PathfindingData _pathfindingData;
    
    private uint _navigationLayers;
    private Vector2? _ignoreFinalPosition = null;

    /// <summary>
    /// Инициализирует Pathfinder
    /// </summary>
    /// <param name="character">Персонаж, для которого делается поиск пути</param>
    /// <param name="navigationAgent">NavigationAgent, лежащий внутри этого персонажа</param>
    /// <param name="navigationService">Ссылка на навигационный сервис в сцене</param>
    /// <param name="pathfindingData">Информация о размере персонажа и его максимальной скорости</param>
    public Pathfinder(Character character, NavigationAgent2D navigationAgent, NavigationService navigationService, PathfindingData pathfindingData)
    {
        _character = character;
        _navigationAgent = navigationAgent;
        _navigationService = navigationService;
        _pathfindingData = pathfindingData;
        
        UpdatePathfindingData(pathfindingData);
        
        // Телепортируем юнита в конечную точку, когда он её почти достиг
        _navigationAgent.TargetReached += () =>  
        {
            _character.GlobalPosition = _navigationAgent.TargetPosition;
        };
        
        // Телепортируем юнита в промежуточную точку пути, если он достаточно близок к ней
        _navigationAgent.WaypointReached += details =>
        {
            _character.GlobalPosition = (Vector2)details["position"];
        };
        
    }
    
    
    /// <summary>
    /// Отвечает на вопрос "в какую СТОРОНУ идти вот прям щас". Можно спрашивать каждый физический такт.
    /// </summary>
    /// <returns>Направление, в котором надо двигаться</returns>
    public Vector2 GetMovementDirection()
    {
        var finalPos = _navigationAgent.GetFinalPosition();
        if (finalPos != _ignoreFinalPosition)
        {
            if (_character.GlobalPosition.DistanceTo(finalPos) <= _navigationAgent.TargetDesiredDistance)
            {
                _character.GlobalPosition = finalPos;
                _ignoreFinalPosition = finalPos;
                _navigationAgent.TargetPosition = finalPos;
            }
        }
        
        var direction = GetAvailableMovement().Normalized();

        return direction;
    }

    /// <summary>
    /// Отвечает на вопрос "в какую ТОЧКУ идти вот прям щас". Можно спрашивать каждый физический такт.
    /// </summary>
    /// <returns>Следующая точка пути, в котором надо двигаться</returns>
    public Vector2 GetNextPathPoint()
    {
        return _navigationAgent.GetNextPathPosition();
    }
    
    /// <summary>
    /// Рекомендуется вызывать этот метод в зависимости от оставшегося расстояния до цели. Чем ближе - тем чаще.
    /// В старой неонке это делалось с постоянной частотой - 1 раз в секунду, и вроде как нормально работало.
    /// </summary>
    /// <param name="globalTargetPosition"></param>
    public void RecalculatePath(Vector2 globalTargetPosition)
    {
        _navigationAgent.TargetPosition = globalTargetPosition;
    }

    /// <summary>
    /// На случай, если поменялись параметры движения персонажа (размер или скорость движения)
    /// </summary>
    /// <param name="pathfindingData"></param>
    public void UpdatePathfindingData(PathfindingData pathfindingData)
    {
        _pathfindingData = pathfindingData;
        _navigationAgent.MaxSpeed = pathfindingData.MaxMovementSpeed;
        _navigationLayers = _navigationService.GetNavigationLayersForSize(_pathfindingData.CharacterRadius);
        
        _navigationAgent.NavigationLayers = _navigationLayers;
    }
    
    private Vector2 GetAvailableMovement()
    {
        var nextPos = GetNextPathPoint();
        var direction = nextPos - _character.GlobalPosition;

        return direction;
    }
}