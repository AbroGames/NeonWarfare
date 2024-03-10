using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class ClientService
{

    [EventListener]
    public void OnServerChangeWorldPacket(ServerChangeWorldPacket serverChangeWorldPacket)
    {
        if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Safe)
        {
            Root.Instance.Game.MainSceneContainer.ChangeStoredNode(Root.Instance.PackedScenes.Main.SafeWorld.Instantiate());
        } 
        else if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Battle)
        {
            Root.Instance.Game.MainSceneContainer.ChangeStoredNode(Root.Instance.PackedScenes.Main.BattleWorld.Instantiate());
        }
        else
        {
            Log.Error($"Received unknown type of MainScene: {serverChangeWorldPacket.WorldType}");
        }
    }
    
    [EventListener]
    public void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        Player player = Root.Instance.CurrentWorld.Player;
        player.Position = Vec(serverSpawnPlayerPacket.X, serverSpawnPlayerPacket.Y);
        player.Rotation = Mathf.DegToRad(serverSpawnPlayerPacket.Dir);
    }
    
    [EventListener]
    public void OnServerWaitBattleEndPacket(ServerWaitBattleEndPacket serverWaitBattleEndPacket)
    {
        if (Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error(
                "OnServerWaitBattleEndPacket, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().MenuContainer.ChangeStoredNode(Root.Instance.PackedScenes.Screen.WaitingForBattleEndScreen.Instantiate());
    }
}