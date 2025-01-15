using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld
{
    
    public ClientPlayer Player { get; set; }
    
    public IReadOnlyDictionary<long, ClientAlly> AlliesById => _alliesById;
    public IEnumerable<ClientAlly> Allies => _alliesById.Values;
    
    private readonly Dictionary<long, ClientAlly> _alliesById = new();
    
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
        if (_alliesById.ContainsKey(ally.AllyProfile.Id)) 
        {
            throw new ArgumentException($"Ally with Id {ally.AllyProfile.Id} already exists.");
        }

        _alliesById[ally.AllyProfile.Id] = ally;
        AddChild(ally);
    }

    public void RemoveAlly(long id)
    {
        _alliesById[id].QueueFree();
        _alliesById.Remove(id);
    }
    
    public void RemoveAlly(ClientAlly ally)
    {
        RemoveAlly(ally.AllyProfile.Id);
    }
    
    public IReadOnlyDictionary<long, ClientAlly> GetAllyByIdExcluding(long excludeId)
    {
        return _alliesById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientAlly> GetAllyExcluding(long excludeId)
    {
        return GetAllyByIdExcluding(excludeId).Values;
    }
    
    public IEnumerable<ClientAlly> GetAllyExcludePlayer()
    {
        return GetAllyExcluding(Player.PlayerProfile.Id);
    }
}