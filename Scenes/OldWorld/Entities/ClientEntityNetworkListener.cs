using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeonWarfare;

public static class ClientEntityNetworkListener
{
    
    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        ClientWorld world = ClientRoot.Instance.Game.World;
        
        Player player = world.CreateAndAddPlayer(ClientRoot.Instance.Game.PlayerProfile);
        player.Position = Vec((float) serverSpawnPlayerPacket.X, (float) serverSpawnPlayerPacket.Y);
        player.Rotation = (float) serverSpawnPlayerPacket.Dir;
        world.OldNetworkEntityManager.AddEntity(player, serverSpawnPlayerPacket.Nid);
		
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

        player.Camera = camera;
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnAllyPacket(ServerSpawnAllyPacket serverSpawnAllyPacket)
    {
        Ally ally = ClientRoot.Instance.PackedScenes.Ally.Instantiate<Ally>();
        ally.Position = Vec((float) serverSpawnAllyPacket.X, (float) serverSpawnAllyPacket.Y);
        ally.Rotation = (float) serverSpawnAllyPacket.Dir;
        ClientRoot.Instance.Game.World.OldNetworkEntityManager.AddEntity(ally, serverSpawnAllyPacket.Nid);
        
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
        Node2D node = ClientRoot.Instance.Game.World.OldNetworkEntityManager.RemoveEntity(serverDestroyEntityPacket.Nid);
        node.QueueFree();
    }
}