using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare.NetOld.Client;

public static class ClientService
{
    
    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        Player player = ClientRoot.Instance.PackedScenes.Player.Instantiate<Player>();
        player.Position = Vec((float) serverSpawnPlayerPacket.X, (float) serverSpawnPlayerPacket.Y);
        player.Rotation = (float) serverSpawnPlayerPacket.Dir;
        ClientRoot.Instance.Game.World.NetworkEntityManager.AddEntity(player, serverSpawnPlayerPacket.Nid);

        ClientWorld world = ClientRoot.Instance.Game.World;
        world.Player = player;
		
        var camera = new Camera();
        camera.Position = player.Position;
        camera.TargetNode = player;
        camera.Zoom = Vec(0.65f);
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
        Ally ally = ClientRoot.Instance.PackedScenes.Ally.Instantiate<Ally>();
        ally.Position = Vec((float) serverSpawnAllyPacket.X, (float) serverSpawnAllyPacket.Y);
        ally.Rotation = (float) serverSpawnAllyPacket.Dir;
        ClientRoot.Instance.Game.World.NetworkEntityManager.AddEntity(ally, serverSpawnAllyPacket.Nid);
        
        ClientWorld world = ClientRoot.Instance.Game.World;
        world.AddChild(ally);
    }
    
    /*[EventListener(ListenerSide.Client)]
    public static void OnServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        Node2D node = ClientRoot.Instance.Game.World.NetworkEntityManager.GetNode<Node2D>(serverPositionEntityPacket.Nid);
        node.Position = Vec(serverPositionEntityPacket.X, serverPositionEntityPacket.Y);
        node.Rotation = serverPositionEntityPacket.Dir;
    }*/

    [EventListener(ListenerSide.Client)]
    public static void OnServerDestroyEntityPacket(ServerDestroyEntityPacket serverDestroyEntityPacket)
    {
        Node2D node = ClientRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(serverDestroyEntityPacket.Nid);
        node.QueueFree();
    }
}