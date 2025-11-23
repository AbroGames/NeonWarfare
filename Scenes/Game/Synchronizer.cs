using System;
using Godot;
using NeonWarfare.Scenes.World.Data.Player;
using NeonWarfare.Scripts.Service.Settings;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// Use for send player data (like nick, color etc.) to server.<br/>
/// We must use synchronizer out of <c>World</c>, because in connecting process <c>World</c> children nodes don't exist.<br/>
/// But we need in synchronizer at connecting process.
/// </summary>
public partial class Synchronizer : Node
{

    public Action SyncStartedOnClientEvent;
    public Action SyncEndedOnClientEvent;
    public Action<string> SyncRejectOnClientEvent;
    
    private World.World _world;
    private PlayerSettings _playerSettings;
    
    [Logger] private ILogger _log;
    
    public Synchronizer InitPreReady(World.World world, PlayerSettings playerSettings)
    {
        Di.Process(this);
        
        if (world == null) _log.Error("World must be not null");
        _world = world;
        
        if (playerSettings == null) _log.Error("PlayerSettings must be not null");
        _playerSettings = playerSettings;

        return this;
    }

    public void StartSyncOnClient()
    {
        SyncStartedOnClientEvent.Invoke();
        NewClientInitOnServer(_playerSettings.Nick, _playerSettings.Color);
    }

    private void NewClientInitOnServer(string nick, Color color) => RpcId(ServerId, MethodName.NewClientInitOnServerRpc, nick, color);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    private void NewClientInitOnServerRpc(string nick, Color color)
    {
        int connectedClientId = GetMultiplayer().GetRemoteSenderId();

        if (_world.TemporaryDataService.PlayerNickByPeerId.Values.Contains(nick))
        {
            RejectSyncOnClient(connectedClientId, "Nickname is already used");
        }
        if (nick.Length < 3 || nick.Length > 25)
        {
            RejectSyncOnClient(connectedClientId, "Lenght of nickname must be between 3 and 25 characters");
        }
        _world.TemporaryDataService.PlayerNickByPeerId.Add(connectedClientId, nick);

        if (!_world.Data.Players.PlayerByNick.ContainsKey(nick))
        {
            PlayerData playerData = new()
            {
                Nick = nick,
                Color = color,
                IsAdmin = nick.Equals(_world.TemporaryDataService.MainAdminNick)
            };
            _world.Data.Players.AddPlayer(playerData);
        }
        
        EndSyncOnClient(connectedClientId, _world.Data.Serializer.SerializeWorldData());
    }

    private void EndSyncOnClient(int id, byte[] serializableData) => RpcId(id, MethodName.EndSyncOnClientRpc, serializableData);
    [Rpc(CallLocal = true)]
    private void EndSyncOnClientRpc(byte[] serializableData)
    {
        _world.Data.Serializer.DeserializeWorldData(serializableData);
        SyncEndedOnClientEvent.Invoke();
    }
    
    private void RejectSyncOnClient(int id, string errorMessage) => RpcId(id, MethodName.RejectSyncOnClientRpc, errorMessage);
    [Rpc(CallLocal = true)] 
    private void RejectSyncOnClientRpc(string errorMessage)
    {
        SyncRejectOnClientEvent.Invoke(errorMessage);
    }
}