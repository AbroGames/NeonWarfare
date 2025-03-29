using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{

    [GamePacket]
    public class CS_WantToBattlePacket(bool want) : BinaryPacket
    {
        public bool WantToBattle = want;
    }
    
    [GamePacket]
    public class CS_AdminGoToBattlePacket : BinaryPacket;
    
    [GamePacket]
    public class CS_PingPacket(long pingId) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
    
        public long PingId = pingId;
    }
    
    [GamePacket]
    public class CS_InitPlayerProfilePacket(string name, Color color) : BinaryPacket
    {
        public string Name = name;
        public Color Color = color;
    }
    
    [GamePacket]
    public class CS_ClientUnlockedAchievementPacket(string achievementId) : BinaryPacket
    {
        public string AchievementId = achievementId;
    }
}
