using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientAlly 
{
    [GamePacket]
    public class SC_ChangeAllyStatsPacket(long peerId, double hp) : BinaryPacket
    {
        public long PeerId = peerId;
        public double Hp = hp;
    }
    
    [GamePacket]
    public class SC_AllyDeadPacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
    
    [GamePacket]
    public class SC_AllyResurrectionPacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
}
