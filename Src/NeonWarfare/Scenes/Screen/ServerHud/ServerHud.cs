using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using Serilog;

namespace NeonWarfare.Scenes.Screen.ServerHud;

/// <summary>
/// Only reads the World: no chat, no commands, no events.
/// </summary>
public partial class ServerHud : Control
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
    [Child] private LineEdit SaveLineEdit { get; set; }
    
    private PlayerQuery _players;
    private readonly ILogger _log = LogFactory.GetForStatic<ServerHud>();
    
    public ServerHud InitPreReady(World.IReader reader)
    {
        if (reader == null) _log.Error("Reader must be not null");
        _players = reader.Get<PlayerQuery>();
        
        return this;
    }

    public override void _Ready()
    {
        Di.Process(this);
    }

    public override void _Process(double delta)
    {
        IEnumerable<string> players = _players.OnlinePlayers()
            .Select(player => $"{player.Nick} (uid: {player.Uid})");
        InfoLabel.Text = "Players:\n" + string.Join("\n", players);
    }
}
