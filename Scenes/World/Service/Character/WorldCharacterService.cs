using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Tree;

namespace NeonWarfare.Scenes.World.Service.Character;

public partial class WorldCharacterService : Node
{
    [SceneService] protected WorldTree Tree;
    [SceneService] protected WorldPersistenceData PersistenceData;
    [SceneService] protected WorldTemporaryData TemporaryData;
    
    public override void _Ready()
    {
        Di.Process(this);
    }
}