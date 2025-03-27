using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.Components;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    public IReadOnlyList<ClientAlly> Allies => _allies;
    public IReadOnlyDictionary<long, ClientAlly> AlliesByPeerId => _alliesByPeerId;
    
    private List<ClientAlly> _allies = new();
    private Dictionary<long, ClientAlly> _alliesByPeerId = new();
    
    public IReadOnlyDictionary<long, ClientAlly> GetAllyProfilesByPeerIdExcluding(long excludePeerId)
    {
        return _alliesByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientAlly> GetAllyProfilesExcluding(long excludePeerId)
    {
        return GetAllyProfilesByPeerIdExcluding(excludePeerId).Values;
    }
    
    public IEnumerable<ClientAlly> GetAllyProfilesExcludePlayer()
    {
        return GetAllyProfilesExcluding(Player.PlayerProfile.PeerId);
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnAllySpawnPacket(SC_AllySpawnPacket allySpawnPacket)
    {
        ClientAlly ally = CreateNetworkEntity<ClientAlly>(
            ClientRoot.Instance.PackedScenes.Ally, allySpawnPacket.Nid);
        ally.AddChild(new NetworkInertiaComponent());
        ally.TreeExiting += () => RemoveAlly(ally);
        
        ally.InitOnProfile(ClientRoot.Instance.Game.AllyProfilesByPeerId[allySpawnPacket.Id]);
        AddChild(ally);
        _allies.Add(ally);
        _alliesByPeerId.Add(allySpawnPacket.Id, ally);
        ally.InitOnSpawnPacket(allySpawnPacket.Position, allySpawnPacket.Rotation, allySpawnPacket.Color);

        ClientRoot.Instance.UnlockAchievement(AchievementIds.BuddyAchievement);
        if (Allies.Count + 1 >= AchievementsPrerequisites.Crowd_PlayersCount)
        {
            ClientRoot.Instance.UnlockAchievement(AchievementIds.CrowdAchievement);
        }
    }

    public void RemoveAlly(ClientAlly ally)
    {
        _allies.Remove(ally);
        _alliesByPeerId.Remove(ally.AllyProfile.PeerId);
    }
}
