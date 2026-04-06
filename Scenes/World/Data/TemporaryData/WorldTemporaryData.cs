using Godot;
using Godot.Collections;
using KludgeBox.Godot.Nodes.MpSync;

namespace NeonWarfare.Scenes.World.Data.TemporaryData;

/// <summary>
/// Session-scoped storage cleared when the game ends.
/// This class only contains data and syncs it over the network.
/// </summary>
public partial class WorldTemporaryData : Node
{
    
    /// <summary>
    /// List of current connected players.
    /// </summary>
    [Export] [Sync] public Dictionary<long, string> PlayerNickByPeerId = new();
    
    public override void _Ready()
    {
        Di.Process(this);
    }
}