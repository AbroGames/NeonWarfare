using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.World.Service.Performance;
using Serilog;

namespace NeonWarfare.Scenes.Screen.ServerHud;

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
    
    private World.World _world;
    [Logger] private ILogger _log;
    
    public ServerHud InitPreReady(World.World world)
    {
        Di.Process(this);
        
        if (world == null) _log.Error("World must be not null");
        _world = world;
        
        return this;
    }

    public override void _Ready()
    {
        Di.Process(this);
        Test1Button.Pressed += () => { _world.Test1(); };
        Test2Button.Pressed += () => { _world.Test2(); };
        Test3Button.Pressed += () => { _world.Test3(); };
        LogButton.Pressed += () => { Services.NodeTree.LogFullTree(_world); };

        SaveButton.Pressed += () => { _world.DataSaveLoadService.Save(SaveLineEdit.Text); };
        
        _world.ChatService.SentNewMessageEvent += message => ChatLabel.Text += $"[{message.Nick}]: {message.Text}\n"; 
        ChatSendButton.Pressed += () => { _world.ChatService.TrySendNewMessage(ChatLineEdit.Text); ChatLineEdit.Clear(); };
    }

    public override void _Process(double delta)
    {
        InfoLabel.Text = _world.PerformanceService.Godot.GetManyLinesString() + "\n" +
                         _world.PerformanceService.Sharp.GetTwoLinesString() + 
                         _world.PerformanceService.ENet.GetTotalInfoOneLineString() + "\n" +
                         GetPlayersENetInfo();
    }
    
    private String GetPlayersENetInfo()
    {
        WorldENetPerformance.PeerInfo defaultPeerInfo = new WorldENetPerformance.PeerInfo(0, 0);
        IEnumerable<String> playersInfo = _world.TemporaryData.PlayerNickByPeerId
            .Select(kv => $"{kv.Value} ({kv.Key}): " + 
                          $"ping {_world.PerformanceService.ENet.InfoByPeerId.GetValueOrDefault((int) kv.Key, defaultPeerInfo).Ping} ms, " + 
                          $"packet loss {_world.PerformanceService.ENet.InfoByPeerId.GetValueOrDefault((int) kv.Key, defaultPeerInfo).PacketLoss:N2}%");
        return "Players:\n" + string.Join("\n", playersInfo);
    }
}