using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerWorld 
{
    
    public IReadOnlyDictionary<long, ServerPlayer> PlayerById => _playerById;
    public IEnumerable<ServerPlayer> Players => _playerById.Values;
    
    private readonly Dictionary<long, ServerPlayer> _playerById = new();

    public void AddEntity(ServerEnemy enemy)
    {
        //TODO реализовать хранилище enemy и способ добавления. Аналогично и для других обхектов (стены, например).
        //TODO Надо бы иметь общий пул всех объектов (для синхронизации, например). И дополнительные пересекающиеся пулы characters, игроков, врагов и т.п.
        //TODO Но ведь уже есть GetChild<> ??? Может пулы вообще не нужны? Достаточно какого-нибудь автовычисляемого свойства ? Маппинг по nid уже есть, маппинг по peerId тоже есть 
        AddChild(enemy);
    }
    
    public void AddPlayer(ServerPlayer player)
    {
        if (_playerById.ContainsKey(player.PlayerProfile.Id)) 
        {
            throw new ArgumentException($"Player with Id {player.PlayerProfile.Id} already exists.");
        }

        _playerById[player.PlayerProfile.Id] = player;
        AddChild(player);
    }

    public void RemovePlayer(long id)
    {
        _playerById[id].QueueFree();
        _playerById.Remove(id);
    }
    
    public void RemovePlayer(ServerPlayer serverPlayer)
    {
        RemovePlayer(serverPlayer.PlayerProfile.Id);
    }
    
    public IReadOnlyDictionary<long, ServerPlayer> GetPlayersByIdExcluding(long excludeId)
    {
        return _playerById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayer> GetPlayersExcluding(long excludeId)
    {
        return GetPlayersByIdExcluding(excludeId).Values;
    }
}