using Godot;
using NeonWarfare.Scenes.World.Data.General;
using NeonWarfare.Scenes.World.Data.MapPoint;
using NeonWarfare.Scenes.World.Data.Player;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.World.Data;

public partial class WorldPersistenceData : Node
{
    
    [Child] public GeneralDataStorage General { get; private set; }
    [Child] public PlayerDataStorage Players { get; private set; }
    [Child] public MapPointDataStorage MapPoint { get; private set; }
    
    public WorldDataSaveLoad SaveLoad;
    public WorldDataSerializer Serializer;
    
    public override void _Ready()
    {
        Di.Process(this);

        SaveLoad = new(this);
        Serializer = new(this);
    }
}