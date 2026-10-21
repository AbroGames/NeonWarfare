using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scenes.World.Entities;

namespace NeonWarfare.Scenes.Game;

public partial class Game : Node2D
{

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes GamePackedScenes { get; set; }
    [Child] private WorldPackedScenes WorldPackedScenes { get; set; }

    private Network.Network _network;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        gameStarter.Init(this);
    }

    public OldWorld.World AddWorld()
    {
        OldWorld.World world = GamePackedScenes.World.Instantiate<OldWorld.World>();
        world.SetName("World");
        WorldContainer.ChangeStoredNode(world);
        return world;
    }
    
    public Hud AddHud()
    {
        Hud hud = GamePackedScenes.Hud.Instantiate<Hud>()
            .InitPreReady(WorldContainer.GetCurrentStoredNode<OldWorld.World>());
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
        return hud;
    }
    
    public ServerHud AddServerHud()
    {
        ServerHud serverHud = GamePackedScenes.ServerHud.Instantiate<ServerHud>()
            .InitPreReady(WorldContainer.GetCurrentStoredNode<OldWorld.World>());
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