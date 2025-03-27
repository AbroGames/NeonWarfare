using System.Linq;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scripts.KludgeBox.Networking;


namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
	
	public ServerWorld World { get; private set; }
	
	public void ChangeAndSendMainScene(ServerWorld serverWorld)
	{
		Network.SendToAll(new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
		Network.SendToAll(new ClientGame.ClientGame.SC_ChangeWorldPacket(serverWorld.GetServerWorldType()));

		ChangeMainScene(serverWorld);
		serverWorld.Init(PlayerProfiles.ToList());
        
		Network.SendToAll(new ClientGame.ClientGame.SC_ClearLoadingScreenPacket());
		Network.SendToAll(new ClientBattleWorld.SC_WaveStartedPacket(1));
	}
	
	private void ChangeMainScene(ServerWorld serverWorld)
	{
		World?.QueueFree();
		World = serverWorld;
		AddChild(World);
	}
}
