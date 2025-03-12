using NeonWarfare.Scenes.World;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
	
	public ServerWorld World { get; private set; }
	
	public void ChangeMainScene(ServerWorld serverWorld)
	{
		World?.QueueFree();
		World = serverWorld;
		AddChild(World);
	}
}
