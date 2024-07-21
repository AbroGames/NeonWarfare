using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ServerGame
{
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
}
