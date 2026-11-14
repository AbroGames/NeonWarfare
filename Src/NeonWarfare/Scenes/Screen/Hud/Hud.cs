using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Screen.Hud;

public partial class Hud : Control
{
    
    [Child] private Label InfoLabel { get; set; }
    
    [Child] private Label ChatLabel { get; set; }
    [Child] private LineEdit ChatLineEdit { get; set; }
    [Child] private Button ChatSendButton { get; set; }
    
    [Child] private Button Test1Button { get; set; }
    [Child] private Button Test2Button { get; set; }
    [Child] private Button Test3Button { get; set; }
    [Child] private Button LogButton { get; set; }
    [Child] private Button SaveButton { get; set; }
    [Child] private Button ExitButton { get; set; }
    [Child] private LineEdit SaveLineEdit { get; set; }
    
    private World.World _world;
    [Logger] private ILogger _log;
    
    public Hud InitPreReady(World.World world)
    {
        Di.Process(this);
        
        if (world == null) _log.Error("World must be not null");
        _world = world;
        
        return this;
    }

    public override void _Ready()
    {
        Di.Process(this);
        
        //TODO 017 chat
        LogButton.Pressed += () => { Services.NodeTree.LogFullTree(_world); };
        ExitButton.Pressed += () => { Services.MainScene.StartMainMenu(); };
    }
}
