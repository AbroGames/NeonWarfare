using System.Collections;
using System.Collections.Generic;
using Godot;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;

namespace NeoVector;

public partial class Server : Node
{
    public ServerParams ServerParams { get; private set; }
    public Cooldown IsParentDeadChecker { get; set; } = new(5);
    
    public IDictionary<long, PlayerServerInfo> PlayerServerInfo { get; private set; } = new Dictionary<long, PlayerServerInfo>();

    public Server(ServerParams serverParams)
    {
        ServerParams = serverParams;
    }

    public override void _Ready()
    {
        EventBus.Publish(new ServerReadyEvent(this));
    }    
    
    public override void _Process(double delta)
    {
        EventBus.Publish(new ServerProcessEvent(this, delta));
    }
}