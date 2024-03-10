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
        Player player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
        player.Position = Vec(serverSpawnPlayerPacket.X, serverSpawnPlayerPacket.Y);
        player.Rotation = serverSpawnPlayerPacket.Dir;
        Root.Instance.NetworkEntityManager.AddEntity(player, serverSpawnPlayerPacket.Nid);

        World world = Root.Instance.CurrentWorld;
        world.Player = player;
		
        var camera = new Camera(); //TODO to camera service
        camera.Position = player.Position;
        camera.TargetNode = player;
        camera.Zoom = Vec(0.65);
        camera.SmoothingPower = 1.5;
        world.AddChild(camera);
        camera.Enabled = true;

        var floor = world.Floor;
        floor.Camera = camera;
        floor.ForceCheck();
        
        world.AddChild(player); // must be here to draw over the floor
    }
    
    [EventListener]
    public void OnServerWaitBattleEndPacket(ServerWaitBattleEndPacket serverWaitBattleEndPacket)
    {
        if (Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error("OnServerWaitBattleEndPacket, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        
        Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().MenuContainer.ChangeStoredNode(
            Root.Instance.PackedScenes.Screen.WaitingForBattleEndScreen.Instantiate());
    }
}