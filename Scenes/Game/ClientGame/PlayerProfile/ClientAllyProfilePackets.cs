using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientAllyProfile
{
    [GamePacket]
    public class SC_ChangeAllyProfilePacket(long peerId, double maxHp, double regenHpSpeed, double movementSpeed, double rotationSpeed) : BinaryPacket
    {
        public long PeerId = peerId;
        public double MaxHp = maxHp;
        public double RegenHpSpeed = regenHpSpeed;
        public double MovementSpeed = movementSpeed;
        public double RotationSpeed = rotationSpeed;
    }
}
