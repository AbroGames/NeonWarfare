using KludgeBox.Events;

namespace NeonWarfare;

public partial class ClientMyPlayer : ClientPlayer 
{
    
    [EventListener(ListenerSide.Client)]
    public static void OnMyPlayerSpawnPacketListener(SC_MyPlayerSpawnPacket myPlayerSpawnPacket)
    {
        ClientMyPlayer myPlayer = ClientRoot.Instance.PackedScenes.MyPlayer.Instantiate<ClientMyPlayer>();
        myPlayer.AddChild(new NetworkEntityComponent(myPlayerSpawnPacket.Nid));
        ClientRoot.Instance.Game.World.NetworkEntityManager.AddEntity(myPlayer);
        
        myPlayer.InitOnProfile(ClientRoot.Instance.Game.MyPlayerProfile);
        ClientRoot.Instance.Game.World.AddMyPlayer(myPlayer);
        myPlayer.OnPlayerSpawnPacket(myPlayerSpawnPacket.X, myPlayerSpawnPacket.Y, myPlayerSpawnPacket.Dir);
    }
}