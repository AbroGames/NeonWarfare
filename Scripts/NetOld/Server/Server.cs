using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;
using NeonWarfare.Net;

namespace NeonWarfare.NetOld.Server;

public partial class Server : Node
{
    public IDictionary<long, PlayerServerInfo> PlayerServerInfo { get; private set; } = new Dictionary<long, PlayerServerInfo>();
    
    public override void _Ready()
    {
        Log.Info("Server ready!");
    }   
}