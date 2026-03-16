using Godot;
using NeonWarfare.Scenes.World.Data.PersistenceData.General;
using NeonWarfare.Scenes.World.Data.PersistenceData.MapPoint;
using NeonWarfare.Scenes.World.Data.PersistenceData.Player;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.World.Data.PersistenceData;

/// <summary>
/// Persistence storage saved on disk when the game ends.
/// This class only contains data and syncs it over the network.
/// </summary>
public partial class WorldPersistenceData : Node
{
    
    [Child] public GeneralDataStorage General { get; private set; }
    [Child] public PlayerDataStorage Players { get; private set; }
    [Child] public MapPointDataStorage MapPoint { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);
    }
}