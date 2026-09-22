using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.OldWorld.Data.TemporaryData;
using NeonWarfare.Scenes.OldWorld.Scenes.SyncedScenes;
using NeonWarfare.Scenes.OldWorld.WorldServices.Characters;

namespace NeonWarfare.Scenes.OldWorld.Tree.Surfaces;

public partial class Surface : Node2D
{
    
    [SceneService] protected WorldTemporaryData TemporaryData;
    
    [SceneService] protected WorldPlayerService PlayerService;
    [SceneService] protected WorldEnemyService EnemyService;
    
    [SceneService] protected SyncedPackedScenes SyncedPackedScenes;
    
    public override void _Ready()
    {
        Di.Process(this);
    }

    public virtual void InitOnServer()
    {
        foreach (int peerId in TemporaryData.PlayerUidByPeerId.Keys)
        {
            PlayerService.SpawnPlayer(peerId);
        }
    }
}