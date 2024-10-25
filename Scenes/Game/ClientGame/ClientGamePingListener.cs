using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;
using NeonWarfare.NetOld.Server;

public partial class ClientGame
{
	[EventListener(ListenerSide.Client)]
	public void ReceivePingPacket(ServerPingPacket serverPingPacket)
	{
		PingChecker.ReceivePingPacket(serverPingPacket.PingId);
	}
}
