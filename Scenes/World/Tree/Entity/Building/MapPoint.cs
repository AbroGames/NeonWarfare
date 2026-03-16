using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.PersistenceData.MapPoint;
using NeonWarfare.Scenes.World.Service.PersistenceFactory;
using NeonWarfare.Scenes.World.Service.StartStop;
using KludgeBox.DI.Requests.SceneServiceInjection;
using KludgeBox.Godot.Nodes.MpSync;
using static Godot.SceneReplicationConfig.ReplicationMode;

namespace NeonWarfare.Scenes.World.Tree.Entity.Building;

public partial class MapPoint : Node2D
{
    
    public MapPointData Data { get; private set; } 

    [Export] [Sync(Never)] private long _id;
    [SceneService] private WorldPersistenceData _persistenceData; 
    
    public override void _EnterTree()
    {
        Di.Process(this);
        
        // Init on client side while client in the game
        // We need second condition, because when client is connecting to the game, Persistence data not synced yet
        if (Data == null && _persistenceData.MapPoint.MapPointById.TryGetValue(_id, out var data))
        {
            InitPreReady(data);
        }
    }

    private void InitPreReady(MapPointData data)
    {
        Data = data;
        Position = Vec2(data.PositionX, data.PositionY);
        _id = data.Id;
    }

    public void UpdatePosition(Vector2 position)
    {
        Position = position;
        Data.PositionX = Position.X;
        Data.PositionY = Position.Y;
    }

    public class Factory : IPersistenceNodeFactory<MapPoint, MapPointData>
    {
        private PackedScene _scene;
        private MapPointDataStorage _storage;
        
        public Type CreatedType()
        {
            return typeof(MapPoint);
        }

        public void InitFactory(World world)
        {
            _scene = world.SyncedPackedScenes.MapPoint;
            _storage = world.PersistenceData.MapPoint;
        }

        public MapPoint Create(Action<MapPointData> init)
        {
            MapPointData mapPointData = new MapPointData();
            init?.Invoke(mapPointData);
            _storage.AddMapPoint(mapPointData);
        
            MapPoint mapPoint = _scene.Instantiate<MapPoint>();
            mapPoint.InitPreReady(mapPointData);
            return mapPoint;
        }
    }

    public class Loader : IWorldTreeLoader
    {
        public const string Name = "MapPoint";
        public string GetName() => Name;
        
        private readonly Dictionary<long, MapPoint> _mapPointById = new();

        public void Create(World world)
        {
            foreach (MapPointData mapPointData in world.PersistenceData.MapPoint.MapPointById.Values)
            {
                MapPoint mapPoint = world.SyncedPackedScenes.MapPoint.Instantiate<MapPoint>();
                world.Tree.MapSurface.AddChildWithUniqueName(mapPoint, "MapPoint");
                _mapPointById.Add(mapPointData.Id, mapPoint);
            }
        }

        public void Init(World world)
        {
            foreach (MapPointData mapPointData in world.PersistenceData.MapPoint.MapPointById.Values)
            {
                MapPoint mapPoint = _mapPointById[mapPointData.Id];
                mapPoint.InitPreReady(mapPointData);
            }
        }
    }
}