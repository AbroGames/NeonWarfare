using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare;

public partial class ClientPlayer : ClientCharacter 
{
    [Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
    
    public ClientPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        PlayerProfile = playerProfile;
    }
    
    public void OnPlayerSpawnPacket(float x, float y, float dir)
    {
        Position = Vec(x, y);
        Rotation = dir;
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnPlayerSpawnPacketListener(SC_PlayerSpawnPacket playerSpawnPacket)
    {
        ClientPlayer player = ClientRoot.Instance.PackedScenes.Player.Instantiate<ClientPlayer>();
        player.AddChild(new NetworkEntityComponent(playerSpawnPacket.Nid));
        ClientRoot.Instance.Game.World.NetworkEntityManager.AddEntity(player);
        
        player.InitOnProfile(ClientRoot.Instance.Game.PlayerProfilesById[playerSpawnPacket.Id]);
        ClientRoot.Instance.Game.World.AddPlayer(player);
        player.OnPlayerSpawnPacket(playerSpawnPacket.X, playerSpawnPacket.Y, playerSpawnPacket.Dir);
    }
}