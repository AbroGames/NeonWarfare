using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using KludgeBox.Godot.Nodes;

namespace NeonWarfare.Scenes.World.MpSpawn;

public partial class WorldMultiplayerSpawner : AbstractMultiplayerSpawner
{
    // We can't use [Child] here, because we must have PackedScenes automatically in multiplayer and before WorldMultiplayerSpawner was added to tree.
    [Export] [NotNull] public PackedScenes.WorldPackedScenes PackedScenes { get; set; }
    [Export] private bool _selfSync = true;
    
    public override IReadOnlyList<PackedScene> GetPackedScenesForSpawn()
    {
        return PackedScenes.GetScenesList();
    }

    public override bool GetSelfSync()
    {
        return _selfSync;
    }

    public override void SetSelfSync(bool selfSync)
    {
        _selfSync = selfSync;
    }
}