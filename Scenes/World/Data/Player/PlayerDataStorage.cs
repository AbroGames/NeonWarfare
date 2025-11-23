using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using MessagePack;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data.Player;

public partial class PlayerDataStorage : Node, ISerializableStorage
{

    [MessagePackObject]
    public record InnerStorage
    {
        [Key(0)] public Dictionary<string, PlayerData> PlayerByNick = new();
    }
    
    public IReadOnlyDictionary<string, PlayerData> PlayerByNick => new ReadOnlyDictionary<string, PlayerData>(_innerStorage.PlayerByNick);
    private InnerStorage _innerStorage = new();

    public void AddPlayer(PlayerData player)
    {
        AddPlayerLocal(player);
        Rpc(MethodName.AddPlayerRpc, Serialize(player));
    }
    
    [Rpc(CallLocal = false)]
    private void AddPlayerRpc(byte[] playerBytes) => AddPlayerLocal(Deserialize<PlayerData>(playerBytes));

    private void AddPlayerLocal(PlayerData player)
    {
        _innerStorage.PlayerByNick[player.Nick] = player;
        SetPropertyListener(player);
    }

    private void SetPropertyListener(PlayerData player)
    {
        player.PropertyChanged += (p, _) => UpdatePlayer((PlayerData) p);
    }
    
    public void RemovePlayer(PlayerData player) => RemovePlayer(player.Nick);
    public void RemovePlayer(string nick) => Rpc(MethodName.RemovePlayerRpc, nick);
    [Rpc(CallLocal = true)]
    private void RemovePlayerRpc(string nick)
    {
        _innerStorage.PlayerByNick.Remove(nick);
    }
    
    private void UpdatePlayer(PlayerData player) => Rpc(MethodName.UpdatePlayerRpc, Serialize(player));
    [Rpc(CallLocal = false)]
    private void UpdatePlayerRpc(byte[] playerBytes)
    {
        PlayerData player = Deserialize<PlayerData>(playerBytes);
        AddPlayerLocal(player);
    }
    
    public byte[] SerializeStorage()
    {
        return Serialize(_innerStorage);
    }

    public void DeserializeStorage(byte[] storageBytes)
    {
        _innerStorage = Deserialize<InnerStorage>(storageBytes);
    }
    
    public void SetAllPropertyListeners()
    {
        foreach (PlayerData player in _innerStorage.PlayerByNick.Values) SetPropertyListener(player);
    }
}