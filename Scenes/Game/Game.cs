using Godot;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scripts.Service.Settings;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;
using NodeContainer = NeonWarfare.Scenes.KludgeBox.NodeContainer;

namespace NeonWarfare.Scenes.Game;

public partial class Game : Node2D
{

    [Child] public NodeContainer WorldContainer { get; set; }
    [Child] public NodeContainer HudContainer { get; set; }
    [Child] public GamePackedScenes PackedScenes { get; set; }

    private Network.Network _network;
    private Synchronizer _synchronizer;

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
            .InitPreReady(WorldContainer.GetCurrentStoredNode<World.World>(), _synchronizer);
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
        return hud;
    }

    public Synchronizer AddSynchronizer(PlayerSettings playerSettings)
    {
        _synchronizer?.QueueFree();
        _synchronizer = new Synchronizer()
            .InitPreReady(WorldContainer.GetCurrentStoredNode<World.World>(), playerSettings);
        this.AddChildWithName(_synchronizer, "Synchronizer");
        return _synchronizer;
    }

    public Network.Network AddNetwork()
    {
        _network?.QueueFree();
        _network = new Network.Network();
        this.AddChildWithName(_network, "Network");
        return _network;
    }
}