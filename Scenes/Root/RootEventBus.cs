using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

public partial class Root
{
	public ServiceRegistry ServiceRegistry { get; private set; } = new();
	
	public void EventBusInit()
	{
		ServiceRegistry.RegisterServices();

		if (OS.GetCmdlineArgs().Contains(ServerParams.ServerFlag))
		{
			EventBus.Side = ListenerSide.Server;
		}
		else
		{
			EventBus.Side = ListenerSide.Client;
		}
		
		EventBus.RegisterListeners(ServiceRegistry);
	}
}