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
/// Only reads the World: no chat, no commands, no events. The log comes from <c>Services.ServerLog</c>.
/// </summary>
public partial class ServerHud : Control
{
    
    [Child] private Label InfoLabel { get; set; }
    [Child] private RichTextLabel LogLabel { get; set; }
    
    private PlayerQuery _players;
    private ServerLogView _logView;
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
        _logView = new ServerLogView(LogLabel, Services.ServerLog);
    }

    public override void _Process(double delta)
    {
        IEnumerable<string> players = _players.GetOnlinePlayers()
            .Select(player => $"{player.Nick} (uid: {player.Uid})");
        InfoLabel.Text = "Players:\n" + string.Join("\n", players);

        _logView.Refresh();
    }
}
