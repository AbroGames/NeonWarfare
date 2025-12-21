using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scenes.World.Services;

public partial class NavigationService : Node2D
{
    /// <summary>
    /// Список стандартных размеров агентов поиска пути.
    /// </summary>
    /// <remarks>Чем их меньше, тем лучше для производительности при запекании карты путей. Допускается не более 32 различных размеров.</remarks>
    public readonly IReadOnlyList<int> UnitSizes = [5, 10, 20, 40, 80, 160];
    public int SmallestUnitSize => UnitSizes[0]; // Самый маленбкий размер в списке
    public int BiggestUnitSize => UnitSizes[^1]; // Самый большой размер в списке
    
    private Dictionary<int, NavigationRegion2D> _regions = new ();
    
    private ILogger _log = LogFactory.GetForStatic<NavigationService>();

    private IEnumerable<Vector2[]> _initialWorldOutlines;
    private IEnumerable<Vector2[]> _initialAdditionalObstacles;
    private Node _initialCollisionsParsingRoot;
    
    private NavigationService(){}
    
    /// <summary>
    /// Перезапечет карты путей
    /// </summary>
    /// <param name="worldOutlines">Координаты углов периметра игровых локаций</param>
    /// <param name="additionalObstacles">Массив полигонов для вырезания в карте путей "дырок", по которым нельзя ходить. Нужен для тонкой настройки карты путей.</param>
    /// <param name="collisionsParsingRoot">Нода, начиная с которой будут рекурсивно парситься статичные тела. По умолчанию парсит прямо с корня сцены.</param>
    /// <remarks>
    /// В <b>worldOutlines</b> можешь передать 4 угла квадрата карты типа [[vec2, vec2, vec2, vec2]]<br/>
    /// <b>additionalObstacles</b> не обязательно, если ты хочешь сделать лишние дырки в карте путей<br/>
    /// <b>collisionsParsingRoot</b> игра может вычислить сама, но для надежности передай сюда ссылку на корень мира
    /// </remarks>
    public NavigationService(IEnumerable<Vector2[]> worldOutlines, IEnumerable<Vector2[]> additionalObstacles = null, Node collisionsParsingRoot = null)
    {
        _initialWorldOutlines = worldOutlines;
        _initialAdditionalObstacles = additionalObstacles;
        _initialCollisionsParsingRoot = collisionsParsingRoot;
    }
    
    public override void _Ready()
    {
        // Предварительно создадим ноды областей навигации для каждого размера
        foreach (var size in UnitSizes)
        {
            var region = new NavigationRegion2D();
            _regions.Add(size, region);
            AddChild(region);
        }
        
        RebuildNavigation(_initialWorldOutlines, _initialAdditionalObstacles, _initialCollisionsParsingRoot);
        _initialWorldOutlines = null;
        _initialAdditionalObstacles = null;
        _initialCollisionsParsingRoot = null;
    }
    

    /// <summary>
    /// Перезапечет карты путей
    /// </summary>
    /// <param name="worldOutlines">Координаты углов периметра игровых локаций</param>
    /// <param name="additionalObstacles">Массив полигонов для вырезания в карте путей "дырок", по которым нельзя ходить. Нужен для тонкой настройки карты путей.</param>
    /// <param name="collisionsParsingRoot">Нода, начиная с которой будут ресурсивно парситься статичные тела. По умолчанию парсит прямо с корня сцены.</param>
    public void RebuildNavigation(IEnumerable<Vector2[]> worldOutlines, IEnumerable<Vector2[]> additionalObstacles = null, Node collisionsParsingRoot = null)
    {
        var navSource = new NavigationMeshSourceGeometryData2D(); // Это контейнер с информацией для запекания карты путей
        
        // Мы создаем полигон-пустышку, чтобы записать в него настройки для парсера препятствий в игровом мире
        var requestPolygon = new NavigationPolygon();
        requestPolygon.ParsedGeometryType = NavigationPolygon.ParsedGeometryTypeEnum.Both;
        requestPolygon.SourceGeometryMode = NavigationPolygon.SourceGeometryModeEnum.RootNodeChildren;
        // Парсим всю недвижимость на карте
        NavigationServer2D.ParseSourceGeometryData(requestPolygon, navSource, collisionsParsingRoot ?? GetTree().Root);

        // Делаем дырки там, где, по задумке, ходить не надо (помимо недвижки)
        if (additionalObstacles is not null)
        {
            foreach (var obstacle in additionalObstacles)
            {
                navSource.AddObstructionOutline(obstacle);
            }
        }
        
        // Запекаем карты путей для всех размеров
        var cachedWorldOutlines = worldOutlines.ToArray();
        int i = 0; // единственная задача этой переменной - сделать сдвиг в битовой маске карты путей.
        foreach (var size in UnitSizes)
        {
            // спавним и настраиваем новый полигон
            var polygon = new NavigationPolygon();
            polygon.AgentRadius = size;
            polygon.ParsedGeometryType = NavigationPolygon.ParsedGeometryTypeEnum.Both;
            polygon.SourceGeometryMode = NavigationPolygon.SourceGeometryModeEnum.RootNodeChildren;
            
            // добавляем все локации в общий полигон
            foreach (var worldOutline in cachedWorldOutlines)
            {
                polygon.AddOutline(worldOutline);
            }
            
            var region = _regions[size];
            region.NavigationLayers = 1u << i; // назначем слой поиска пути для соответствующего размера
            
            BakeNavMesh(polygon, navSource, region); // ВЫПЕЧКА
            i++;
        }
    }

    /// <summary>
    /// Запускает запекание карты путей.
    /// </summary>
    /// <param name="navigationPolygon">Ссылка на полигон (пустой) карты путей, в который будут запечены пути</param>
    /// <param name="navSource">Контейнер с информацией о проходимых и непроходимых участках</param>
    /// <param name="targetRegion">Карта путей, в которую нужно положить полигон после запекания</param>
    private void BakeNavMesh(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D navSource, NavigationRegion2D targetRegion)
    {
        // обратный вызов из асинхронно запекаемой карты пути, чтобы назначить хлебобулочный полигон в соответствующий NavigationRegion2D
        var cb = () =>
        {
            targetRegion.NavigationPolygon = navigationPolygon;
            _log.Information("Baked NavMesh for layer {layer}", $"{targetRegion.NavigationLayers:b8}");
        };
        NavigationServer2D.BakeFromSourceGeometryDataAsync(navigationPolygon, navSource, Callable.From(cb));
    }
    
    /// <summary>
    /// Округляет входной радиус до ближайшего большего из имеющихся в наборе <see cref="UnitSizes"/>
    /// </summary>
    /// <param name="rawSize">Исходный размер юнита</param>
    /// <returns>Ближайший размер, по карту путей которого этот юнит сможет ходить</returns>
    public int GetUnitSizeFor(float rawSize)
    {
        int unitSize;
        rawSize = Mathf.RoundToInt(rawSize);

        try
        {
            unitSize = UnitSizes.First(size => size >= rawSize);
        }
        catch (InvalidOperationException e)
        {
            _log.Error("Unable to map raw size of {rawSize} to any of existing ones: [{SmallestUnitSize} ... {BiggestUnitSize}]", 
                $"{rawSize:N2}", SmallestUnitSize, BiggestUnitSize);
            unitSize = UnitSizes[^1]; 
        }
        
        return unitSize;
    }
    
    /// <summary>
    /// Возвращает маску поиска пути по которой должен ориентироваться юнит с указанным размером
    /// </summary>
    /// <param name="size">Размер (радиус) юнита</param>
    /// <returns>Маска поиска пути, подходящая этому юниту</returns>
    public uint GetNavigationLayersForSize(float size)
    {
        return _regions[GetUnitSizeFor(size)].NavigationLayers;
    }

    /// <summary>
    /// Бесполезный мусор, пока что
    /// </summary>
    /// <returns>Маска, соответствующая комбинации всех существующих масок поиска пути</returns>
    public uint GetAvoidanceLayers()
    {
        uint avoidance = 0;
        foreach (var (_, region) in _regions)
        {
            avoidance |= region.NavigationLayers;
        }
        return avoidance;
    }
}