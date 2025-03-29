using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scripts.KludgeBox.Networking;


namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
	public ServerWorld World { get; private set; }
	private void GoToBattleWorld()
	{
		ServerBattleWorld battleWorld = ServerRoot.Instance.PackedScenes.BattleWorld.Instantiate<ServerBattleWorld>();
		ChangeAndSendMainScene(battleWorld);
	}
	
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
