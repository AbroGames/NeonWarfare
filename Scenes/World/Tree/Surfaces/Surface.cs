using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service.Characters;

namespace NeonWarfare.Scenes.World.Tree.Surfaces;

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
        foreach (int peerId in TemporaryData.PlayerNickByPeerId.Keys)
        {
            PlayerService.SpawnPlayer(peerId);
        }
    }
}