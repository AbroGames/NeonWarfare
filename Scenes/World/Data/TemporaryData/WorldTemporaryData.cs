using Godot;
using KludgeBox.Godot.Nodes.MpSync;

namespace NeonWarfare.Scenes.World.Data.TemporaryData;

/// <summary>
/// Session-scoped storage cleared when the game ends.
/// This class only contains data and syncs it over the network.
/// </summary>
public partial class WorldTemporaryData : Node
{
    /// <summary>
    /// Hoster nick, or nick from cmd param in dedicated server.<br/>
    /// <c>Player.IsAdmin</c> in <c>WorldPersistenceData</c> for this player automatically will change to true.<br/>
    /// If next application start will be with <c>MainAdminNick = null</c>, then <c>Player.IsAdmin</c>
    /// in <c>WorldPersistenceData</c> change to false on this player connecting process.<br/>
    /// </summary>
    [Export] [Sync] public string MainAdminNick; 
    
    /// <summary>
    /// List of current connected players.
    /// </summary>
    [Export] [Sync] public Godot.Collections.Dictionary<long, string> PlayerNickByPeerId = new();
    
    public override void _Ready()
    {
        Di.Process(this);
    }
}