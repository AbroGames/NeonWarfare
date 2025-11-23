using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using MessagePack;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data.MapPoint;

public partial class MapPointDataStorage : Node, ISerializableStorage
{

    [MessagePackObject]
    public record InnerStorage
    {
        [Key(0)] public long NextId = 1; // Id=0 was reserved for uninitialized object
        [Key(1)] public Dictionary<long, MapPointData> MapPointById = new();
    }
    
    public IReadOnlyDictionary<long, MapPointData> MapPointById => new ReadOnlyDictionary<long, MapPointData>(_innerStorage.MapPointById);
    private InnerStorage _innerStorage = new();

    public void AddMapPoint(MapPointData mapPoint)
    {
        if (mapPoint.Id == 0) mapPoint.Id = _innerStorage.NextId++;
        
        AddMapPointLocal(mapPoint);
        Rpc(MethodName.AddMapPointRpc, Serialize(mapPoint));
    }
    
    [Rpc(CallLocal = false)]
    private void AddMapPointRpc(byte[] mapPointBytes) => AddMapPointLocal(Deserialize<MapPointData>(mapPointBytes));

    private void AddMapPointLocal(MapPointData mapPoint)
    {
        _innerStorage.MapPointById[mapPoint.Id] = mapPoint;
        SetPropertyListener(mapPoint);
    }

    private void SetPropertyListener(MapPointData mapPoint)
    {
        mapPoint.PropertyChanged += (m, _) => UpdateMapPoint((MapPointData) m);
    }
    
    public void RemoveMapPoint(MapPointData mapPoint) => RemoveMapPoint(mapPoint.Id);
    public void RemoveMapPoint(long id) => Rpc(MethodName.RemoveMapPointRpc, id);
    [Rpc(CallLocal = true)]
    private void RemoveMapPointRpc(long id)
    {
        _innerStorage.MapPointById.Remove(id);
    }
    
    private void UpdateMapPoint(MapPointData mapPoint) => Rpc(MethodName.UpdateMapPointRpc, Serialize(mapPoint));
    [Rpc(CallLocal = false)]
    private void UpdateMapPointRpc(byte[] mapPointBytes)
    {
        MapPointData mapPoint = Deserialize<MapPointData>(mapPointBytes);
        AddMapPointLocal(mapPoint);
    }
    
    public byte[] SerializeStorage()
    {
        return Serialize(_innerStorage);
    }

    public void DeserializeStorage(byte[] storageBytes)
    {
        _innerStorage = Deserialize<InnerStorage>(storageBytes);
    }
    
    public void SetAllPropertyListeners()
    {
        foreach (MapPointData mapPoint in _innerStorage.MapPointById.Values) SetPropertyListener(mapPoint);
    }
}