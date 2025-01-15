using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld
{
    
    public ClientPlayer Player { get; set; }
    
    public IReadOnlyDictionary<long, ClientAlly> AlliesByPeerId => _alliesByPeerId;
    public IEnumerable<ClientAlly> Allies => _alliesByPeerId.Values;
    
    private readonly Dictionary<long, ClientAlly> _alliesByPeerId = new();
    
    public void AddPlayer(ClientPlayer player)
    {
        if (Player != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        AddAlly(player);
        Player = player;
    }

    public void RemovePlayer()
    {
        RemoveAlly(Player);
        Player = null;
    }

    public void AddAlly(ClientAlly ally)
    {
        if (_alliesByPeerId.ContainsKey(ally.AllyProfile.PeerId)) 
        {
            throw new ArgumentException($"Ally with PeerId {ally.AllyProfile.PeerId} already exists.");
        }

        _alliesByPeerId[ally.AllyProfile.PeerId] = ally;
        AddChild(ally);
    }

    public void RemoveAlly(long id)
    {
        _alliesByPeerId[id].QueueFree();
        _alliesByPeerId.Remove(id);
    }
    
    public void RemoveAlly(ClientAlly ally)
    {
        RemoveAlly(ally.AllyProfile.PeerId);
    }
    
    public IReadOnlyDictionary<long, ClientAlly> GetAllyByPeerIdExcluding(long excludePeerId)
    {
        return _alliesByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientAlly> GetAllyExcluding(long excludePeerId)
    {
        return GetAllyByPeerIdExcluding(excludePeerId).Values;
    }
    
    public IEnumerable<ClientAlly> GetAllyExcludePlayer()
    {
        return GetAllyExcluding(Player.PlayerProfile.PeerId);
    }
}