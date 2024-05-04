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
        NeonWarfare.Root.Instance.NetworkEntityManager.Clear();
        
        if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Safe)
        {
            NeonWarfare.Root.Instance.MainSceneContainer.ChangeStoredNode(NeonWarfare.Root.Instance.PackedScenes.Main.SafeWorld.Instantiate());
        } 
        else if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Battle)
        {
            NeonWarfare.Root.Instance.MainSceneContainer.ChangeStoredNode(NeonWarfare.Root.Instance.PackedScenes.Main.BattleWorld.Instantiate());
        }
        else
        {
            Log.Error($"Received unknown type of MainScene: {serverChangeWorldPacket.WorldType}");
        }
    }
    
    [EventListener]
    public void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        NeonWarfare.Player player = NeonWarfare.Root.Instance.PackedScenes.World.Player.Instantiate<NeonWarfare.Player>();
        player.Position = Vec(serverSpawnPlayerPacket.X, serverSpawnPlayerPacket.Y);
        player.Rotation = serverSpawnPlayerPacket.Dir;
        NeonWarfare.Root.Instance.NetworkEntityManager.AddEntity(player, serverSpawnPlayerPacket.Nid);

        NeonWarfare.World world = NeonWarfare.Root.Instance.CurrentWorld;
        world.Player = player;
		
        var camera = new NeonWarfare.Camera(); //TODO to camera service
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
    public void OnServerSpawnAllyPacket(ServerSpawnAllyPacket serverSpawnAllyPacket)
    {
        NeonWarfare.Ally ally = NeonWarfare.Root.Instance.PackedScenes.World.Ally.Instantiate<NeonWarfare.Ally>();
        ally.Position = Vec(serverSpawnAllyPacket.X, serverSpawnAllyPacket.Y);
        ally.Rotation = serverSpawnAllyPacket.Dir;
        NeonWarfare.Root.Instance.NetworkEntityManager.AddEntity(ally, serverSpawnAllyPacket.Nid);
        
        NeonWarfare.World world = NeonWarfare.Root.Instance.CurrentWorld;
        world.AddChild(ally);
    }
    
    [EventListener]
    public void OnServerWaitBattleEndPacket(ServerWaitBattleEndPacket serverWaitBattleEndPacket)
    {
        if (NeonWarfare.Root.Instance.MainSceneContainer.GetCurrentStoredNode<Node>() is not NeonWarfare.MainMenuMainScene)
        {
            Log.Error("OnServerWaitBattleEndPacket, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        
        NeonWarfare.Root.Instance.MainSceneContainer.GetCurrentStoredNode<NeonWarfare.MainMenuMainScene>().ChangeMenu(
            NeonWarfare.Root.Instance.PackedScenes.Screen.WaitingForBattleEndScreen);
    }
    
    [EventListener]
    public void OnServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        Node2D node = NeonWarfare.Root.Instance.NetworkEntityManager.GetNode<Node2D>(serverPositionEntityPacket.Nid);
        node.Position = Vec(serverPositionEntityPacket.X, serverPositionEntityPacket.Y);
        node.Rotation = serverPositionEntityPacket.Dir;
    }

    [EventListener]
    public void OnServerDestroyEntityPacket(ServerDestroyEntityPacket serverDestroyEntityPacket)
    {
        Node2D node = NeonWarfare.Root.Instance.NetworkEntityManager.RemoveEntity(serverDestroyEntityPacket.Nid);
        node.QueueFree();
    }
}