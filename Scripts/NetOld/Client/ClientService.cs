using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare.NetOld.Client;

public static class ClientService
{

    [EventListener(ListenerSide.Client)]
    public static void OnServerChangeWorldPacket(ServerChangeWorldPacket serverChangeWorldPacket)
    {
        ClientRoot.Instance.Game.NetworkEntityManager.Clear();
        
        if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Safe)
        {
            ClientRoot.Instance.Game.ChangeMainScene(ClientRoot.Instance.PackedScenes.GameMainScenes.SafeWorld.Instantiate<SafeGameMainScene>());
        } 
        else if (serverChangeWorldPacket.WorldType == ServerChangeWorldPacket.ServerWorldType.Battle)
        {
            ClientRoot.Instance.Game.ChangeMainScene(ClientRoot.Instance.PackedScenes.GameMainScenes.BattleWorld.Instantiate<BattleGameMainScene>());
        }
        else
        {
            Log.Error($"Received unknown type of MainScene: {serverChangeWorldPacket.WorldType}");
        }
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        Player player = ClientRoot.Instance.PackedScenes.World.Player.Instantiate<Player>();
        player.Position = Vec(serverSpawnPlayerPacket.X, serverSpawnPlayerPacket.Y);
        player.Rotation = serverSpawnPlayerPacket.Dir;
        ClientRoot.Instance.Game.NetworkEntityManager.AddEntity(player, serverSpawnPlayerPacket.Nid);

        World world = ClientRoot.Instance.Game.World;
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

    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnAllyPacket(ServerSpawnAllyPacket serverSpawnAllyPacket)
    {
        Ally ally = ClientRoot.Instance.PackedScenes.World.Ally.Instantiate<Ally>();
        ally.Position = Vec(serverSpawnAllyPacket.X, serverSpawnAllyPacket.Y);
        ally.Rotation = serverSpawnAllyPacket.Dir;
        ClientRoot.Instance.Game.NetworkEntityManager.AddEntity(ally, serverSpawnAllyPacket.Nid);
        
        World world = ClientRoot.Instance.Game.World;
        world.AddChild(ally);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnServerWaitBattleEndPacket(ServerWaitBattleEndPacket serverWaitBattleEndPacket)
    {
        if (ClientRoot.Instance.MainMenu is null)
        {
            Log.Error("OnServerWaitBattleEndPacket, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        
        ClientRoot.Instance.MainMenu.ChangeMenu(ClientRoot.Instance.PackedScenes.Screen.WaitingForBattleEndScreen);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        Node2D node = ClientRoot.Instance.Game.NetworkEntityManager.GetNode<Node2D>(serverPositionEntityPacket.Nid);
        node.Position = Vec(serverPositionEntityPacket.X, serverPositionEntityPacket.Y);
        node.Rotation = serverPositionEntityPacket.Dir;
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerDestroyEntityPacket(ServerDestroyEntityPacket serverDestroyEntityPacket)
    {
        Node2D node = ClientRoot.Instance.Game.NetworkEntityManager.RemoveEntity(serverDestroyEntityPacket.Nid);
        node.QueueFree();
    }
}