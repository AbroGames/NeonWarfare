using Godot;
using KludgeBox;
using KludgeBox.Networking;

namespace NeonWarfare;

public partial class ServerPlayer : ServerCharacter
{
    public ServerPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ServerPlayerProfile playerProfile)
    {
        PlayerProfile = playerProfile;
        //TODO MaxHp = maxHp и т.д, у клиента аналогично
    }

    public void Init()
    {
        Position = Vec(400, 400); //TODO временно для теста, потом удалить и у следующей строки убрать знак +
        Position += Vec(Rand.Range(-100, 100), Rand.Range(-100, 100)); 
        Rotation = Mathf.DegToRad(Rand.Range(0, 360));
        
        //У нового игрока спауним его самого
        Network.SendToClient(PlayerProfile.Id, 
            new ClientMyPlayer.SC_MyPlayerSpawnPacket(Nid, Position.X, Position.Y, Rotation));
        
        //У всех остальных игроков спауним нового игрока
        Network.SendToAllExclude(PlayerProfile.Id, new ClientPlayer.SC_PlayerSpawnPacket(Nid, Position.X, Position.Y, Rotation, PlayerProfile.Id));
    }

    public static ServerPlayer CreateAndSpawn(ServerPlayerProfile playerProfile) //TODO в readme о том, что мы такое стараемся использовать или убрать нахер
    {
        ServerPlayer player = ServerRoot.Instance.PackedScenes.Player.Instantiate<ServerPlayer>();        
        player.AddChild(new NetworkEntityComponent());
        ServerRoot.Instance.Game.World.NetworkEntityManager.AddEntity(player);
        
        player.InitOnProfile(playerProfile);
        ServerRoot.Instance.Game.World.AddPlayer(player);
        player.Init();
        
        return player;
    }
}