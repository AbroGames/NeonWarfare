using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.MapGenerator;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public partial class ServerBattleWorld : ServerWorld
{
    public EnemyWave EnemyWave { get; private set; }
    private EnemySpawner _enemySpawner;
    private AutoCooldown _timeSyncCooldown = new(1);
    
    public override void _Ready()
    {
        base._Ready();

        _enemySpawner = new(this);
        EnemyWave = new(_enemySpawner);
        _timeSyncCooldown.ActionWhenReady += () =>
        {
            Network.SendToAll(new ClientBattleWorld.ClientBattleWorld.SC_WaveTimeSyncPacket(EnemyWave.NextWaveCooldown.TimeLeft));
        };
    }
    
    protected override void InitMap()
    {
        MapGenerator mapGenerator = new MapGenerator();
        List<Vector2[]> locationMeshes = new(); // Список локаций для которых нам надо будет запечь карту путей
        
        foreach (var location in mapGenerator.Generate())
        {
            foreach (var entity in location.Entities)
            {
                InitEntity(entity);
                Network.SendToAll(entity);
            }

            var coords = location.GetBorderCoordinates().ToArray();
            Network.SendToAll(new ClientWorld.SC_LocationMesh(coords)); // отправляем дебаг-пакет клиентам, чтобы они смогли построить у себя ПРИМЕРНУЮ карту путей.
            locationMeshes.Add(coords);
            Log.Debug($"Adding location mesh: {string.Join(' ', coords)}");
        }
        
        // Генерируем и запекаем карты путей
        NavigationService.RebuildNavigation(
            worldOutlines: locationMeshes,
            additionalObstacles: null, // сюда можно добавить "непроходимые" области, помимо просто стен, которые парсятся автоматически
            collisionsParsingRoot: this // начиная с этой ноды будут рекурсивно парситься стены
            );
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        EnemyWave.Update(delta);
        _enemySpawner.Update(delta);
        _timeSyncCooldown.Update(delta);
    }
    
    public override WorldInfoStorage.WorldType GetServerWorldType()
    {
        return WorldInfoStorage.WorldType.Battle;
    }
}
