using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.PersistenceData.Player;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Service.Character;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using Serilog;

namespace NeonWarfare.Scenes.World.Service;

/// <summary>
/// Use for send player data (like nick, color etc.) to server.<br/>
/// Use in multiplayer and singleplayer games for init player info.
/// </summary>
public partial class WorldSynchronizerService : Node
{
    private const int NicknameMinLength = 3;
    private const int NicknameMaxLength = 25;
    private static readonly string LengthOfNicknameErrorMessage = $"Length of nickname must be between {NicknameMinLength} and {NicknameMaxLength} characters";
    private const string NicknameAlreadyUsedErrorMessage = "Nickname is already used";
    private const string NicknameContainsSpaceErrorMessage = "Nickname contains space";
    
    public event Action SyncStartedOnClientEvent;
    public event Action SyncEndedOnClientEvent;
    public event Action<string> SyncRejectOnClientEvent;
    
    public event Action<int> SyncEndedOnServerEvent;

    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    [SceneService] private WorldDataSerializerService _dataSerializerService;
    [SceneService] private WorldPlayerService _playerService;
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void StartSyncOnClient(string nick, Color color)
    {
        _log.Information("Starting sync with server as '{nick}'", nick);
        SyncStartedOnClientEvent?.Invoke();
        NewClientInitOnServer(nick, color);
    }

    private void NewClientInitOnServer(string nick, Color color) => RpcId(ServerId, MethodName.NewClientInitOnServerRpc, nick, color);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    private void NewClientInitOnServerRpc(string nick, Color color)
    {
        int connectedClientId = GetMultiplayer().GetRemoteSenderId();
        _log.Information("Peer {peer} attempting to sync using nick '{nick}'", connectedClientId, nick);

        if (_temporaryData.PlayerNickByPeerId.Values.Contains(nick))
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, NicknameAlreadyUsedErrorMessage);
            RejectSyncOnClient(connectedClientId, NicknameAlreadyUsedErrorMessage);
        }
        if (nick.Length < NicknameMinLength || nick.Length > NicknameMaxLength)
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, LengthOfNicknameErrorMessage);
            RejectSyncOnClient(connectedClientId, LengthOfNicknameErrorMessage);
        }
        if (nick.Contains(' '))
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, NicknameContainsSpaceErrorMessage);
            RejectSyncOnClient(connectedClientId, NicknameContainsSpaceErrorMessage);
        }
        
        _temporaryData.PlayerNickByPeerId.Add(connectedClientId, nick);

        if (!_persistenceData.Players.PlayerByNick.ContainsKey(nick))
        {
            _persistenceData.Players.AddPlayer(new PlayerData
            {
                Nick = nick,
                Color = color
            });
        }
        PlayerData playerData = _persistenceData.Players.PlayerByNick[nick];
        playerData.IsAdmin |= nick.Equals(_temporaryData.MainAdminNick);
        _log.Information("Player data from peer {peer} was synced", connectedClientId);
        
        _log.Information("Sending serialized world data to peer {peer}", connectedClientId);
        EndSyncOnClient(connectedClientId, _dataSerializerService.SerializeWorldData());
    }

    private void EndSyncOnClient(long peerId, byte[] serializableData) => RpcId(peerId, MethodName.EndSyncOnClientRpc, serializableData);
    [Rpc(CallLocal = true)]
    private void EndSyncOnClientRpc(byte[] serializableData)
    {
        _log.Information("Received serialized world data from server");
        _dataSerializerService.DeserializeWorldData(serializableData);
        EndSyncOnServer();
        _log.Information("Syncing complete successfully");
        
        SyncEndedOnClientEvent?.Invoke();
    }
    
    private void RejectSyncOnClient(long peerId, string errorMessage) => RpcId(peerId, MethodName.RejectSyncOnClientRpc, errorMessage);
    [Rpc(CallLocal = true)] 
    private void RejectSyncOnClientRpc(string errorMessage)
    {
        _log.Error("Syncing with the server was rejected with error: {error}", errorMessage);
        SyncRejectOnClientEvent?.Invoke(errorMessage);
    }
    
    private void EndSyncOnServer() => RpcId(ServerId, MethodName.EndSyncOnServerRpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    private void EndSyncOnServerRpc()
    {
        int connectedClientId = GetMultiplayer().GetRemoteSenderId();
        _log.Information("Syncing peer {peer} completed successfully", connectedClientId );
        
        _playerService.SpawnPlayer(connectedClientId);
        SyncEndedOnServerEvent?.Invoke(connectedClientId);
    }
}