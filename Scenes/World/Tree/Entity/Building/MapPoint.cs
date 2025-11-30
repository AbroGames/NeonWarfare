using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Godot.Nodes.MpSync;
using NeonWarfare.Scenes.World.Data.MapPoint;
using NeonWarfare.Scenes.World.Services.PersistenceFactory;
using NeonWarfare.Scenes.World.Services.StartStop;
using static Godot.SceneReplicationConfig.ReplicationMode;

namespace NeonWarfare.Scenes.World.Tree.Entity.Building;

public partial class MapPoint : Node2D
{
    
    public MapPointData Data { get; private set; } 

    [Export] [Sync(Never)] private long _id;
    [Parent(true)] private World _world; 
    
    public override void _EnterTree()
    {
        Di.Process(this);
        
        // Init on client side
        if (Data == null) InitPreReady(_world.Data.MapPoint.MapPointById[_id]);
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
            _scene = world.WorldPackedScenes.MapPoint;
            _storage = world.Data.MapPoint;
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
            foreach (MapPointData mapPointData in world.Data.MapPoint.MapPointById.Values)
            {
                MapPoint mapPoint = world.WorldPackedScenes.MapPoint.Instantiate<MapPoint>();
                world.Tree.MapSurface.AddChildWithUniqueName(mapPoint, "MapPoint");
                _mapPointById.Add(mapPointData.Id, mapPoint);
            }
        }

        public void Init(World world)
        {
            foreach (MapPointData mapPointData in world.Data.MapPoint.MapPointById.Values)
            {
                MapPoint mapPoint = _mapPointById[mapPointData.Id];
                mapPoint.InitPreReady(mapPointData);
            }
        }
    }
}