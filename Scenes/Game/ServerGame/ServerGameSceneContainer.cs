using NeonWarfare.Scenes.World;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
	
	public ServerWorld World { get; private set; }
	
	//TODO мб переместить сюда? Network.SendToAll(new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle));
	public void ChangeMainScene(ServerWorld serverWorld)
	{
		World?.QueueFree();
		World = serverWorld;
		AddChild(World);
	}
}
