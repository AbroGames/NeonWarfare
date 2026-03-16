using Godot;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Game;

public partial class Game : Node2D
{

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes PackedScenes { get; set; }

    private Network.Network _network;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        gameStarter.Init(this);
    }

    public World.World AddWorld()
    {
        World.World world = PackedScenes.World.Instantiate<World.World>();
        world.SetName("World");
        WorldContainer.ChangeStoredNode(world);
        return world;
    }
    
    public Hud AddHud()
    {
        Hud hud = PackedScenes.Hud.Instantiate<Hud>()
            .InitPreReady(WorldContainer.GetCurrentStoredNode<World.World>());
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
        return hud;
    }
    
    public ServerHud AddServerHud()
    {
        ServerHud serverHud = PackedScenes.ServerHud.Instantiate<ServerHud>()
            .InitPreReady(WorldContainer.GetCurrentStoredNode<World.World>());
        serverHud.SetName("ServerHud");
        HudContainer.ChangeStoredNode(serverHud);
        return serverHud;
    }

    public Network.Network AddNetwork()
    {
        _network?.QueueFree();
        _network = new Network.Network(this);
        this.AddChildWithName(_network, "Network");
        return _network;
    }
}