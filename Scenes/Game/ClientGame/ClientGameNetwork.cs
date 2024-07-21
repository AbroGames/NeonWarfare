using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
}
